"""Build the office in Blender, using the menu painting to register the camera and paper artwork.

Usage: blender --background --python build_office.py -- <reference.png> <output-directory>
The .blend keeps the individually editable props. The .glb batches meshes for Godot.
"""

import math
from pathlib import Path
from random import Random
import sys

import bpy
import numpy as np
from mathutils import Euler, Matrix, Quaternion, Vector


WIDTH, HEIGHT = 1656, 950
DESK = 0.82
CREDENZA = 0.92
WALL = 2.79
FOCAL = 1145
PHONE_RECEIVER = "Curved Bakelite receiver"
rng = Random(61463)
# New props draw from their own stream so the registered desk props keep their placement.
prop_rng = Random(1960)


def linear(c):
    return c / 12.92 if c < 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def color(hex_rgb):
    return tuple(linear(int(hex_rgb[i:i + 2], 16) / 255) for i in (0, 2, 4))


def material(name, hex_rgb, roughness=.45, metal=0, maps=None):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color(hex_rgb), 1)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metal
    mat.diffuse_color = (*color(hex_rgb), 1)
    if maps:
        albedo, microsurface, relief = maps
        nodes, links = mat.node_tree.nodes, mat.node_tree.links
        base_texture = nodes.new("ShaderNodeTexImage")
        base_texture.image = albedo
        base_texture.location = (-580, 320)
        links.new(base_texture.outputs["Color"], bsdf.inputs["Base Color"])
        rough_texture = nodes.new("ShaderNodeTexImage")
        rough_texture.image = microsurface
        rough_texture.location = (-580, 30)
        channels = nodes.new("ShaderNodeSeparateColor")
        channels.location = (-280, 20)
        links.new(rough_texture.outputs["Color"], channels.inputs["Color"])
        links.new(channels.outputs["Green"], bsdf.inputs["Roughness"])
        normal_texture = nodes.new("ShaderNodeTexImage")
        normal_texture.image = relief
        normal_texture.location = (-580, -250)
        normal = nodes.new("ShaderNodeNormalMap")
        normal.location = (-280, -250)
        links.new(normal_texture.outputs["Color"], normal.inputs["Color"])
        links.new(normal.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def packed_image(name, rgb, data=False):
    height, width, _ = rgb.shape
    # Pack an sRGB PNG for Blender and glTF.
    encoded = rgb if data else np.where(rgb <= .0031308, rgb * 12.92, 1.055 * np.maximum(rgb, 0) ** (1 / 2.4) - .055)
    rgba = np.ones((height, width, 4), dtype=np.float32)
    rgba[..., :3] = np.clip(encoded, 0, 1)
    img = bpy.data.images.new(name, width, height, alpha=False)
    if data:
        img.colorspace_settings.name = "Non-Color"
    img.pixels.foreach_set(rgba.ravel())
    img.pack()
    return img


class TextureField:
    """Sample repeatable noise and small marks in the same UV space as the finish."""

    def __init__(self, width, height, seed):
        self.width, self.height = width, height
        self.random = np.random.default_rng(seed)
        self.u, self.v = np.meshgrid(np.arange(width, dtype=np.float32) / width,
                                     np.arange(height, dtype=np.float32) / height)

    def noise(self, nx, ny, u=None, v=None):
        grid = self.random.random((ny, nx), dtype=np.float32)
        u, v = (self.u if u is None else u) * nx, (self.v if v is None else v) * ny
        u, v = np.mod(u, nx), np.mod(v, ny)
        i, j = u.astype(int), v.astype(int)
        s, t = u - i, v - j
        s, t = s * s * (3 - 2 * s), t * t * (3 - 2 * t)
        return ((grid[j, i] * (1 - s) + grid[j, (i + 1) % nx] * s) * (1 - t)
                + (grid[(j + 1) % ny, i] * (1 - s) + grid[(j + 1) % ny, (i + 1) % nx] * s) * t).astype(np.float32)

    def marks(self, count, length, width, direction=None):
        marks = np.zeros_like(self.u)
        for _ in range(count):
            cx, cy = self.random.uniform((0, 0), (self.width, self.height))
            a = self.random.uniform(*length) * self.width
            b = self.random.uniform(*width) * self.height
            angle = self.random.uniform(0, math.tau) if direction is None else self.random.normal(direction, .18)
            dx, dy = math.cos(angle), math.sin(angle)
            reach_x, reach_y = abs(dx)*a + abs(dy)*b, abs(dy)*a + abs(dx)*b
            xs = np.arange(math.floor(cx-reach_x), math.ceil(cx+reach_x)+1)
            ys = np.arange(math.floor(cy-reach_y), math.ceil(cy+reach_y)+1)
            x, y = np.meshgrid(xs-cx, ys-cy)
            r2 = ((dx*x+dy*y)/a)**2 + ((-dy*x+dx*y)/b)**2
            pixels = np.clip(1-r2, 0, 1)**2 * self.random.uniform(.25, 1)
            cells = np.ix_(ys % self.height, xs % self.width)
            marks[cells] = np.maximum(marks[cells], pixels)
        return marks

    def pebble(self, nx, ny):
        x, y = self.u * nx, self.v * ny
        i, j = x.astype(int), y.astype(int)
        offsets = self.random.uniform(.20, .80, (ny, nx, 2)).astype(np.float32)
        nearest = np.full_like(self.u, np.inf)
        second = nearest.copy()
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                sites = offsets[(j+dy) % ny, (i+dx) % nx]
                d2 = (x - (i+dx+sites[..., 0]))**2 + (y - (j+dy+sites[..., 1]))**2
                second = np.minimum(second, np.maximum(nearest, d2))
                nearest = np.minimum(nearest, d2)
        return np.clip((np.sqrt(second) - np.sqrt(nearest)) * 3.3, 0, 1)


def palette(dark, light, tone):
    return np.array(color(dark)) + np.clip(tone, 0, 1)[..., None] * (np.array(color(light)) - np.array(color(dark)))


def finish(name, rgb, roughness, height, relief_scale):
    # Tangent-space normals from the same recesses, grain and scratches as the color and roughness.
    dx = (np.roll(height, 1, axis=1) - np.roll(height, -1, axis=1)) * relief_scale
    dy = (np.roll(height, 1, axis=0) - np.roll(height, -1, axis=0)) * relief_scale
    length = np.sqrt(1 + dx*dx + dy*dy)
    normal = .5 + .5 * np.stack((dx/length, dy/length, 1/length), axis=-1)
    return (packed_image(f"{name} • Color", rgb),
            packed_image(f"{name} • Roughness", np.repeat(roughness[..., None], 3, axis=2), data=True),
            packed_image(f"{name} • Normal", normal, data=True))


def walnut_finish(name, width, height, seed, dark, light, figured=False):
    field = TextureField(width, height, seed)
    u, v = field.u, field.v
    warp = v + .065*(field.noise(4, 8) - .5) + .016*(field.noise(8, 17) - .5)
    heart = np.zeros_like(v)
    if figured:
        # Growth lines crowd and bend around small knots in the flat-sawn veneer.
        for x, y, sx, sy in ((.35, .39, .17, .059), (.72, .68, .15, .048), (.19, .81, .12, .040)):
            du, dv = (u-x+.5) % 1 - .5, (v-y+.5) % 1 - .5
            falloff = np.exp(-((du/sx)**2+(dv/sy)**2))
            warp += .037*np.tanh(dv/(sy*.38))*falloff
            heart = np.maximum(heart, np.exp(-((du/(sx*.19))**2+(dv/(sy*.20))**2)))
    warm = field.noise(5, 14, v=warp)
    bands = .5 + .5*np.cos(math.tau*(34*warp + .25*field.noise(7, 43)))
    fibers = field.noise(21, 240, v=warp)
    pores = np.clip((.32-field.noise(125, 370, v=warp))*3.4, 0, 1)**2 * (.25+.75*bands)
    scratches = field.marks(150 if figured else 38, (.002, .020), (.0007, .0022), .10)
    rubbed = field.noise(14, 24)
    tone = .21+.41*warm+.19*fibers-.14*bands**8-.24*pores-.39*heart+.22*scratches
    rough = (.45 if figured else .57) + .115*(rubbed-.5)+.14*pores+.20*scratches
    height = .26*fibers-.40*pores-.16*bands**8-.21*scratches-.22*heart
    return finish(name, palette(dark, light, tone), rough, height, .40 if figured else .31)


def leather_finish(name, dark, light, seed, worn=False):
    field = TextureField(1024 if worn else 512, 1024 if worn else 512, seed)
    pebble = field.pebble(112 if worn else 92, 155 if worn else 86)
    wrinkles = field.pebble(27, 41)
    fiber = field.noise(290, 292)
    dye = field.noise(12, 16)
    wear = np.zeros_like(pebble)
    scars = np.zeros_like(pebble)
    fold = np.zeros_like(pebble)
    if worn:
        u, v = field.u, field.v
        edge = np.minimum.reduce((u, v, 1-u, 1-v))
        worn_patches = field.noise(15, 13)
        wear = (.23 + .77*worn_patches)*np.exp(-(edge/.045)**2)
        fold = np.exp(-((u-.89-.015*np.sin(v*math.tau*2))/.014)**2) * (.25+.75*worn_patches)
        scars = field.marks(180, (.0025, .030), (.0008, .003), -.5)
    tone = .27+.28*dye+.14*pebble-.14*(1-wrinkles)**6+.37*wear+.20*scars+.12*fold
    rough = (.68+.065*(dye-.5)+.10*(1-pebble)+.14*scars-.09*wear
             if worn else .43+.14*(dye-.5)+.16*(1-pebble))
    relief = .54*pebble+.12*wrinkles+.10*fiber-.22*scars-.26*fold
    return finish(name, palette(dark, light, tone), rough, relief, .44 if worn else .29)


def hardware_finish(name, dark, light, base_roughness, seed, brush=False, oxidized=False):
    field = TextureField(512, 512, seed)
    cloud = field.noise(10, 12)
    micro = field.noise(13, 278) if brush else field.noise(134, 153)
    scars = field.marks(95, (.006, .065), (.0012, .003), .06 if brush else None)
    pitting = np.clip((.32-field.noise(80, 88))*3.3, 0, 1)**2
    tone = .54+.19*(cloud-.5)+.18*(micro-.5)+.36*scars
    roughness = base_roughness + .12*(cloud-.5)+.12*(micro-.5)+.22*scars
    if oxidized:
        oxide = np.clip((field.noise(12, 16)-.48)*2.2, 0, 1) * (.45+.55*field.noise(52, 49))
        tone -= .52*oxide+.18*pitting
        roughness += .22*oxide
    return finish(name, palette(dark, light, tone), roughness, .15*micro-.14*pitting-.22*scars, .37)


def bakelite_finish():
    field = TextureField(768, 512, 296)
    warp = field.v+.18*(field.noise(5, 7)-.5)
    resin = field.noise(9, 22, v=warp)
    flecks = field.noise(185, 142)
    polish = field.noise(19, 16)
    scuffs = field.marks(110, (.003, .025), (.0013, .0034))
    tone = .12+.49*resin+.24*field.noise(27, 38, v=warp)+.09*flecks+.12*scuffs
    return finish("Molded honey Bakelite", palette("6d4829", "c29860", tone),
                  .28+.13*(polish-.5)+.19*scuffs, .08*flecks-.19*scuffs, .38)


def paper_finish(name, field, rgb, ruled=False):
    u, v = field.u, field.v
    edge = np.minimum.reduce((u, v, 1-u, 1-v))
    pulp = field.noise(190, 242)
    fibers = field.marks(1050, (.001, .0045), (.0005, .0017))
    foxing = field.marks(380, (.0009, .0045), (.0009, .0042)) * (.3+.7*np.exp(-edge/.08))
    age = .065*np.exp(-edge/.032) + .048*field.noise(12, 16) + .16*foxing
    rgb = rgb * (1 + .05*(pulp-.5) - .12*fibers - age)[..., None]
    if ruled:
        row = np.exp(-((v*27 - np.rint(v*27))/.035)**2) * (v > .025) * (v < .975) * (u > .01) * (u < .99)
        for column in (.775, .865, .920, .963):
            row = np.maximum(row, np.exp(-((u-column)/.0013)**2) * (v > .005) * (v < .976))
        margin = np.maximum.reduce([np.exp(-((u-column)/.0014)**2) for column in (.12, .698, .704)])
        margin *= (v > .005) * (v < .975)
        for ink, mask in (("5e7785", row), ("a05a48", margin)):
            amount = (mask*(.85+.13*pulp))[..., None]
            rgb = rgb*(1-amount) + np.array(color(ink))*amount
    return finish(name, rgb, .77+.13*pulp, .22*pulp-.13*fibers, .23)


def cut_paper_finish():
    field = TextureField(512, 256, 472)
    layers = .5+.5*np.cos(math.tau*(31*field.v + .28*field.noise(19, 54)))
    fiber = field.noise(168, 88)
    tone = .54+.16*fiber-.31*layers**4
    return finish("Compressed page edges", palette("73583d", "c3a681", tone),
                  .77+.13*fiber, .16*fiber-.25*layers**4, .35)


def tile_floor_finish():
    """Nine-inch vinyl-asbestos tiles laid in a checker, each with its own marbling direction and wax wear."""
    tiles = 4
    field = TextureField(1024, 1024, 1958)
    u, v = field.u * tiles, field.v * tiles
    i, j = u.astype(int), v.astype(int)
    fu, fv = u - i, v - j
    checker = (i + j) % 2
    per_tile = field.random.random((tiles, tiles)).astype(np.float32)[j % tiles, i % tiles]
    # Alternate tiles turn their marbling a quarter, as tile setters laid them.
    across = np.where(checker == 1, fu, fv)
    along = np.where(checker == 1, fv, fu)
    marble = (.5 + .5*np.sin(math.tau*(5.5*across + 2.6*field.noise(5, 5) + 1.1*field.noise(13, 13) + .5*along + per_tile*3))) ** 12
    chips = np.clip((field.noise(240, 240) - .63) * 4.2, 0, 1)
    edge = np.minimum.reduce((fu, fv, 1-fu, 1-fv))
    seam = np.exp(-(edge/.0075)**2)
    traffic = field.noise(3, 3)
    scuffs = field.marks(260, (.002, .018), (.0006, .0018), .35)
    tone = .42 + .16*(per_tile-.5) + .16*(field.noise(11, 11)-.5) + .26*marble - .18*chips + .10*traffic
    dark = palette("272a22", "4d5043", tone)
    light = palette("4d4334", "86755a", tone)
    rgb = np.where(checker[..., None] == 1, light, dark)
    rgb *= (1 - .55*seam - .22*scuffs)[..., None]
    rough = .30 + .22*(1-traffic) + .10*(field.noise(31, 31)-.5) + .25*scuffs + .35*seam
    return finish("Waxed marbled floor tile", rgb, rough, .06*marble - .55*seam - .14*scuffs, .32)


def rug_finish():
    """A worn wool rug: a madder field with an all-over lattice, a navy and gold border, and a walked-flat path."""
    field = TextureField(768, 1024, 2214)
    u, v = field.u, field.v
    edge = np.minimum.reduce((u*1.33, v, (1-u)*1.33, 1-v))
    pile = field.noise(300, 400)
    lattice = np.abs(np.sin(math.tau*(7*u + 5*v))) * np.abs(np.sin(math.tau*(7*u - 5*v)))
    motif = np.clip(1 - lattice*3.2, 0, 1)
    border = (edge < .085) & (edge > .022)
    guard = np.exp(-((edge-.095)/.006)**2) + np.exp(-((edge-.016)/.004)**2)
    worn = np.exp(-(((u-.5)/.30)**2 + ((v-.45)/.36)**2)) * (.55 + .45*field.noise(9, 12))
    tone = .38 + .30*pile + .14*field.noise(17, 22) - .12*worn
    rgb = palette("3d1511", "8e3626", tone)
    rgb = rgb*(1-.55*motif[..., None]) + palette("6f5a37", "b38e4f", tone)*(.55*motif[..., None])
    rgb = np.where(border[..., None], palette("141a2a", "34415e", tone), rgb)
    border_motif = border & (np.abs(np.sin(math.tau*np.where(u*1.33 < v, u, v)*48)) * np.abs(np.sin(math.tau*edge*28)) > .72)
    rgb = np.where(border_motif[..., None], palette("5a4930", "8f7445", tone), rgb)
    rgb = rgb*(1-.6*np.clip(guard, 0, 1)[..., None]) + palette("6f5a37", "c3a265", tone)*(.6*np.clip(guard, 0, 1)[..., None])
    rgb = rgb*(1 + .30*worn[..., None]) * (1 - .18*worn[..., None]) + .06*worn[..., None]
    return finish("Worn madder wool rug", rgb, .90 + .08*(pile-.5) - .10*worn, .35*pile - .10*worn, .45)


def ceiling_tile_finish():
    """Twelve-inch fissured acoustic tiles, yellowed by years of cigarettes."""
    tiles = 4
    field = TextureField(1024, 1024, 1131)
    u, v = field.u * tiles, field.v * tiles
    fu, fv = u - u.astype(int), v - v.astype(int)
    edge = np.minimum.reduce((fu, fv, 1-fu, 1-fv))
    bevel_edge = np.clip(1 - edge/.025, 0, 1) ** 2
    fissures = field.marks(2600, (.0015, .009), (.0006, .0016))
    pits = np.clip((.30 - field.noise(180, 180)) * 3.5, 0, 1) ** 2
    nicotine = field.noise(4, 4)
    tone = .62 + .10*(field.noise(40, 40)-.5) - .45*fissures - .30*pits - .35*bevel_edge - .16*nicotine
    rgb = palette("6c6150", "e8dfc8", tone)
    return finish("Fissured acoustic ceiling tile", rgb, .93 + 0*tone, -.55*fissures - .30*pits - .45*bevel_edge, .40)


def painted_steel_finish(name, dark, light, seed):
    """Baked enamel over steel: orange peel, handling smudges and edge chips down to grey metal."""
    field = TextureField(512, 512, seed)
    peel = field.noise(150, 150)
    smudge = field.noise(9, 7)
    chips = np.clip((field.marks(70, (.002, .010), (.0015, .006)) - .35) * 2.4, 0, 1)
    tone = .55 + .16*(smudge-.5) + .07*(peel-.5)
    rgb = palette(dark, light, tone)*(1-chips[..., None]) + np.array(color("3d3f3d"))*chips[..., None]
    return finish(name, rgb, .38 + .14*(smudge-.5) + .06*peel + .20*chips, .10*peel - .25*chips, .30)


def fiberglass_finish():
    """Molded fiberglass shell: a glossy olive gel coat with the glass strands showing through."""
    field = TextureField(768, 768, 4044)
    strands = field.marks(2200, (.006, .030), (.0005, .0011))
    cloud = field.noise(8, 8)
    tone = .42 + .22*(cloud-.5) + .30*strands + .06*(field.noise(90, 90)-.5)
    return finish("Olive fiberglass gel coat", palette("2a301d", "667049", tone),
                  .26 + .08*(cloud-.5) + .10*strands, .12*strands, .22)


def artwork(name, corners, width, height):
    """Rectify one original printed surface to UV space. Geometry supplies its perspective."""
    matrix, rhs = [], []
    for (u, v), (x, y) in zip(((0, 0), (1, 0), (1, 1), (0, 1)), corners):
        matrix.extend(([u, v, 1, 0, 0, 0, -u*x, -v*x], [0, 0, 0, u, v, 1, -u*y, -v*y]))
        rhs.extend((x, y))
    a, b, c, d, e, f, g, h = np.linalg.solve(matrix, rhs)
    u, v = np.meshgrid(np.linspace(.002, .998, width), np.linspace(.998, .002, height))
    x = np.clip((a*u + b*v + c) / (g*u + h*v + 1), 0, WIDTH - 2)
    y = np.clip(HEIGHT - 1 - (d*u + e*v + f) / (g*u + h*v + 1), 0, HEIGHT - 2)
    ix, iy = x.astype(int), y.astype(int)
    fx, fy = (x - ix)[..., None], (y - iy)[..., None]
    rgb = ((reference[iy, ix] * (1 - fx) + reference[iy, ix + 1] * fx) * (1 - fy)
           + (reference[iy + 1, ix] * (1 - fx) + reference[iy + 1, ix + 1] * fx) * fy)
    rgb = np.where(rgb <= .04045, rgb / 12.92, ((rgb + .055) / 1.055) ** 2.4)
    grey = rgb.mean(axis=-1, keepdims=True)
    rgb = (grey + .72*(rgb - grey)) * .90
    field = TextureField(width, height, sum(map(ord, name)))
    return material(name, "bca481", maps=paper_finish(name, field, rgb))


def group(name):
    global collection
    collection = bpy.data.collections.new(name)
    scene.collection.children.link(collection)


def link(obj, name, mat=None):
    if collection not in obj.users_collection:
        for old in list(obj.users_collection):
            old.objects.unlink(obj)
        collection.objects.link(obj)
    obj.name = name
    if mat:
        obj.data.materials.append(mat)
    return obj


def mesh(name, vertices, faces, mat, uv=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    link(obj, name, mat)
    if uv:
        layer = data.uv_layers.new()
        for loop in data.loops:
            layer.data[loop.index].uv = uv[loop.vertex_index]
    return obj


def bevel(obj, radius, segments=3):
    if radius:
        modifier = obj.modifiers.new("Soft edges", "BEVEL")
        modifier.width = radius
        modifier.segments = segments
        modifier = obj.modifiers.new("Corner normals", "WEIGHTED_NORMAL")
        modifier.keep_sharp = True
    return obj


def box(name, loc, dimensions, mat, radius=.005, angle=0, grain=None, smooth=False, grain_scale=(1.35, .56)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = link(bpy.context.object, name, mat)
    for vertex in obj.data.vertices:
        for axis in range(3):
            vertex.co[axis] *= dimensions[axis]
    obj.rotation_euler.z = angle
    if smooth:
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    u_axis, v_axis = grain or (0, 2)
    other_axis = 3-u_axis-v_axis
    for polygon in obj.data.polygons:
        u_face = other_axis if abs(polygon.normal[u_axis]) > .5 else u_axis
        v_face = other_axis if abs(polygon.normal[v_axis]) > .5 else v_axis
        if grain:
            u_scale, v_scale = grain_scale
            u_offset, v_offset = .51 + loc[u_axis]*.68, .51 + loc[v_axis]*.83
        else:
            u_scale, v_scale = dimensions[u_face], dimensions[v_face]
            u_offset = v_offset = .5
        for i in polygon.loop_indices:
            co = obj.data.vertices[obj.data.loops[i].vertex_index].co
            obj.data.uv_layers.active.data[i].uv = (co[u_face]/u_scale+u_offset, co[v_face]/v_scale+v_offset)
    return bevel(obj, radius)


def ellipsoid(name, loc, radii, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=1, location=loc)
    obj = link(bpy.context.object, name, mat)
    obj.scale = radii
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj


def cyl(name, a, b, radius, mat, r2=None, sides=32):
    a, b = Vector(a), Vector(b)
    delta = b - a
    bpy.ops.mesh.primitive_cone_add(vertices=sides, radius1=radius, radius2=radius if r2 is None else r2,
                                   depth=delta.length, location=(a + b) / 2)
    obj = link(bpy.context.object, name, mat)
    obj.rotation_euler = delta.to_track_quat("Z", "Y").to_euler()
    for poly in obj.data.polygons:
        poly.use_smooth = len(poly.vertices) == 4
    return bevel(obj, min(.002, radius * .2), 2)


def tube(name, points, radius, mat):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = radius
    curve.bevel_resolution = 3
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for point, co in zip(spline.points, points):
        point.co = (*co, 1)
    return link(bpy.data.objects.new(name, curve), name, mat)


def ring_uv(obj, sides, rows, caps=0):
    layer = obj.data.uv_layers.new()
    for poly in obj.data.polygons:
        if poly.index < caps:
            for i in poly.loop_indices:
                angle = math.tau*(obj.data.loops[i].vertex_index % sides)/sides
                layer.data[i].uv = (.5+.48*math.cos(angle), .5+.48*math.sin(angle))
            continue
        row, segment = divmod(poly.index - caps, sides)
        u0, u1 = segment/sides, (segment+1)/sides
        uv = ((u0, rows[row]), (u1, rows[row]), (u1, rows[row+1]), (u0, rows[row+1]))
        for i, coord in zip(poly.loop_indices, uv):
            layer.data[i].uv = coord


def lathe(name, position, profile, mat, sides=96, y_scale=1, axis=(0, 0, 1), notch=0):
    rotation = Vector(axis).to_track_quat("Z", "Y")
    points = []
    for radius, height in profile:
        for i in range(sides):
            a = i * math.tau / sides
            cut = sum(math.exp(-((math.atan2(math.sin(a - key), math.cos(a - key))) / .095) ** 2)
                      for key in (.24, 2.38, 4.65))
            dip = notch * cut * max(0, 1 - abs(height - .059) / .027)
            points.append(Vector(position) + rotation @ Vector((radius * math.cos(a), radius * math.sin(a) * y_scale, height - dip)))
    faces = [(j*sides + i, j*sides + (i+1) % sides, (j+1)*sides + (i+1) % sides, (j+1)*sides + i)
             for j in range(len(profile) - 1) for i in range(sides)]
    obj = mesh(name, points, faces, mat)
    length = np.r_[0, np.cumsum([math.dist(a, b) for a, b in zip(profile, profile[1:])])]
    ring_uv(obj, sides, length/length[-1])
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return obj


def ray(u, v):
    return forward + right * ((u - WIDTH / 2) / FOCAL) + up * ((HEIGHT / 2 - v) / FOCAL)


def at_z(u, v, z):
    r = ray(u, v)
    return camera_pos + r * ((z - camera_pos.z) / r.z)


def at_y(u, v, y):
    r = ray(u, v)
    return camera_pos + r * (y / r.y)


def at_plane(u, v, point, normal):
    r = ray(u, v)
    return camera_pos + r * (normal.dot(point - camera_pos) / normal.dot(r))


def projected_quad(corners, z):
    return [at_z(*point, z) for point in corners]


def panel(name, quad, thickness, top, edge=None, radius=0, rounded=0):
    a, b, c, d = map(Vector, quad)
    normal = (d - a).cross(b - a).normalized()
    uv = [(0, 0), (1, 0), (1, 1), (0, 1)]
    if rounded:
        uv = []
        for x, y, start in ((rounded, rounded, 180), (1-rounded, rounded, 270),
                            (1-rounded, 1-rounded, 0), (rounded, 1-rounded, 90)):
            for step in range(6):
                angle = math.radians(start + step*18)
                uv.append((x + rounded*math.cos(angle), y + rounded*math.sin(angle)))
    outline = [interpolate_quad((a, b, c, d), u, v) for u, v in uv]
    count = len(outline)
    verts = outline + [v - normal * thickness for v in outline]
    faces = [tuple(reversed(range(count))), tuple(range(count, 2*count))]
    for i in range(count):
        j = (i+1) % count
        faces.append((i, j, j+count, i+count))
    obj = mesh(name, verts, faces, top, [(u, 1-v) for u, v in uv] * 2)
    if edge:
        obj.data.materials.append(edge)
        obj.data.polygons[1].material_index = 1
        perimeter = 0
        for i, face in enumerate(list(obj.data.polygons)[2:]):
            face.material_index = 1
            next_perimeter = perimeter + (outline[(i+1) % count]-outline[i]).length/.28
            for loop, coord in zip(face.loop_indices, ((perimeter, 1), (next_perimeter, 1),
                                                      (next_perimeter, 0), (perimeter, 0))):
                obj.data.uv_layers.active.data[loop].uv = coord
            perimeter = next_perimeter
    return bevel(obj, radius)


def interpolate_quad(quad, u, v):
    a, b, c, d = quad
    return a.lerp(b, u).lerp(d.lerp(c, u), v)


def area(name, loc, target, power, tint, size):
    data = bpy.data.lights.new(name, "AREA")
    data.energy, data.color, data.shape, data.size = power, tint, "DISK", size
    obj = link(bpy.data.objects.new(name, data), name)
    obj.location = loc
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def room():
    door_left = at_y(282, 210, WALL - .035).x
    door_right = at_y(647, 215, WALL - .035).x
    door_width = door_right - door_left
    group("Walnut paneling and door casing")
    for i in range(-16, 7):
        left, right = i * .235 - .116, i * .235 + .116
        inside_left, inside_right = max(left, door_left), min(right, door_right)
        spans = [(left, right, 0, 2.5)]
        if inside_left < inside_right:
            spans = [(left, inside_left, 0, 2.5), (inside_right, right, 0, 2.5),
                     (inside_left, inside_right, 2.2, 2.5)]
        for a, b, bottom, top in spans:
            if b-a > .001:
                box("Walnut wall panel", ((a+b)/2, WALL+.014, (bottom+top)/2-.05 if bottom == 0 else (bottom+top)/2),
                    (b-a, .063, top-bottom), walnuts[i % len(walnuts)], .002, grain=(2, 0))
    for x in (door_left, door_right):
        box("Dark steel door casing", (x, WALL - .086, 1.11), (.026, .039, 2.23), gunmetal, .003)
        box("Door jamb", (x, WALL+.013, 1.10), (.026, .15, 2.20), frame_wood, .003, grain=(2, 0))
    box("Door header", ((door_left+door_right)/2, WALL-.045, 2.216), (door_width+.030, .16, .036),
        frame_wood, .004, grain=(0, 2))
    box("Scuffed door threshold", ((door_left+door_right)/2, WALL, .004), (door_width, .15, .012), tarnished_brass, .002)
    switch = at_y(720, 116, WALL - .029)
    box("Light switch faceplate", switch, (.077, .012, .137), tarnished_brass, .005)
    box("Toggle", switch + Vector((0, -.012, -.004)), (.012, .017, .032), ivory, .003).rotation_euler.x = -.30
    for dz in (-.052, .052):
        cyl("Switch faceplate screw", switch + Vector((0, -.005, dz)), switch + Vector((0, -.009, dz)), .005, nickel, sides=12)
    box("Marbled tile floor", (-.5, 1.65, -.035), (6, 5, .06), floor_tile, .002, grain=(0, 1), grain_scale=(.9144, .9144))
    box("Wool rug", (-.45, .80, .004), (3.05, 2.30, .008), rug, .003)

    group("Office door leaf")
    hinge = link(bpy.data.objects.new("OfficeDoorHinge", None), "OfficeDoorHinge")
    hinge.location = (door_left, WALL-.04, 0)
    box("Office door", ((door_left + door_right) / 2, WALL - .04, 1.1),
        (door_width, .055, 2.2), walnuts[1], .004, grain=(2, 0))
    knob = at_y(607, 251, WALL - .115)
    cyl("Knob escutcheon", knob + Vector((0, .047, 0)), knob, .043, nickel)
    cyl("Knob stem", knob, knob + Vector((0, -.027, 0)), .019, brass)
    ellipsoid("Round door knob", knob + Vector((0, -.043, 0)), (.035, .023, .035), nickel)
    cyl("Hall knob escutcheon", (knob.x, WALL-.012, knob.z), (knob.x, WALL+.002, knob.z), .043, nickel)
    cyl("Hall knob stem", (knob.x, WALL+.002, knob.z), (knob.x, WALL+.041, knob.z), .017, brass)
    ellipsoid("Hall knob", (knob.x, WALL+.053, knob.z), (.035, .026, .035), nickel)
    for z in (.30, 1.12, 1.88):
        cyl("Door hinge pin", (door_left+.01, WALL-.076, z-.032), (door_left+.01, WALL-.076, z+.032), .009, tarnished_brass)
    for obj in list(collection.objects):
        if obj != hinge:
            obj.parent = hinge
            obj.matrix_parent_inverse = Matrix.Translation(-hinge.location)

    intro_set(door_left, door_right)

    group("Desk and credenza")
    desk_back = at_z(828, 484, DESK).y
    box("Polished walnut desk top", (-.5, (desk_back + .24) / 2, DESK - .037),
        (3.35, desk_back - .24, .074), desk_wood, .012, grain=(0, 1), grain_scale=(3.35, 1.54))
    box("Desk apron", (-.5, (desk_back + .28) / 2, DESK - .145),
        (3.25, desk_back - .28, .16), walnuts[2], .012, grain=(0, 2))
    # Two drawer pedestals on recessed plinths. The visitor's side stays open: the guest chair tucks in between.
    pedestal_width, front = .44, .29
    depth = desk_back - .025 - front
    height = DESK - .074 - .045
    for left_edge in (-2.10, 1.10 - pedestal_width):
        cx = left_edge + pedestal_width/2
        box("Pedestal plinth", (cx, front + depth/2 + .02, .0225), (pedestal_width-.05, depth-.05, .045), dark_wood, .004)
        box("Desk pedestal", (cx, front + depth/2, .045 + height/2), (pedestal_width, depth, height), walnuts[2], .008,
            grain=(2, 0))
        top = .045 + height - .012
        for drawer_height in (.165, .190, height - .405):
            z = top - drawer_height/2
            box("Drawer front", (cx, front - .010, z), (pedestal_width - .036, .022, drawer_height - .014),
                walnuts[(int(z*100)) % 3], .006, grain=(0, 2))
            pull_z = z + (drawer_height/2 - .045 if drawer_height > .3 else 0)
            for dx in (-.055, .055):
                cyl("Pull post", (cx+dx, front-.021, pull_z), (cx+dx, front-.044, pull_z), .0055, brass, sides=12)
            cyl("Brass drawer pull", (cx-.075, front-.046, pull_z), (cx+.075, front-.046, pull_z), .0075, brass, sides=16)
            if drawer_height > .3:
                box("Card holder", (cx, front-.023, z+drawer_height/2-.085), (.090, .004, .040), tarnished_brass, .002)
                box("Index card", (cx, front-.0255, z+drawer_height/2-.085), (.074, .002, .027), paper, .0005)
            top -= drawer_height
    front = at_z(990, 406, CREDENZA).y
    left = at_y(663, 375, front).x
    w = 2.30
    box("Credenza top", (left + w / 2, (front + WALL) / 2, CREDENZA - .022),
        (w, WALL - front, .044), desk_wood, .008, grain=(0, 1))
    box("Credenza case", (left + w / 2, (front + WALL) / 2 + .025, .434),
        (w - .045, WALL - front - .075, .88), walnuts[2], .007, grain=(0, 2))
    for i in range(4):
        center = left + .290 + i * .57
        box("Inset cabinet door", (center, front + .031, .48), (.540, .023, .78), walnuts[i % 3], .005, grain=(2, 0))
        cyl("Credenza pull", (center - .23, front + .008, .62), (center - .23, front + .008, .78), .008, tarnished_brass)

    group("Olive fiberglass guest chair")
    # The painting's shell chair, seen from behind across the desk: one molded seat and back on a steel rod base.
    shell_chair(at_z(423, 457, .80).x - .037, 1.80, -.075)

    group("Desk swivel chair")
    # A 1960 executive tilt-swivel: channel-tufted leather back, chrome arms, five-star base on casters.
    x, y, _ = camera_pos
    cy = y - .03

    def plump(obj, level=1):
        obj.modifiers.new("Plump", "SUBSURF").levels = level
        return obj

    plump(box("Leather seat cushion", (x, cy, .49), (.54, .50, .11), leather, .045, smooth=True))
    tilt = Euler((-.15, 0, 0)).to_matrix()
    back_center = Vector((x, cy-.255, .875))
    back = plump(box("Channel-tufted leather back", back_center, (.50, .085, .52), leather, .040, smooth=True))
    back.rotation_euler.x = -.15
    for dx in (-.125, 0, .125):
        tube("Tufting channel", [back_center + tilt @ Vector((dx, .044, dz)) for dz in (-.21, 0, .21)], .0045, edge_leather)
    tube("Back crown roll", [back_center + tilt @ Vector((dx, .030, .255)) for dx in (-.22, 0, .22)], .020, leather)
    for side in (-1, 1):
        ax = x + side*.300
        tube("Chrome arm", [(ax, cy+.17, .45), (ax, cy+.20, .58), (ax, cy+.13, .655), (ax, cy-.16, .655),
             (ax, cy-.21, .60), (x+side*.24, cy-.23, .52)], .0105, nickel)
        plump(box("Leather arm pad", (ax, cy-.015, .675), (.065, .31, .035), leather, .014, smooth=True))
    cyl("Tilt spring housing", (x, cy, .36), (x, cy, .43), .070, gunmetal)
    cyl("Back support bar", (x, cy-.06, .40), (x, cy-.25, .66), .020, gunmetal)
    cyl("Swivel column", (x, cy, .12), (x, cy, .37), .030, nickel)
    for i in range(5):
        a = i * math.tau/5 + math.pi/2
        foot = Vector((x + .30*math.cos(a), cy + .30*math.sin(a), .075))
        cyl("Base spoke", (x, cy, .13), foot, .022, gunmetal, r2=.015)
        cyl("Caster fork", foot, foot - Vector((0, 0, .03)), .010, nickel, sides=12)
        cyl("Caster wheel", foot + Vector((-.018, 0, -.045)), foot + Vector((.018, 0, -.045)), .027, rubber, sides=20)
    eye = link(bpy.data.objects.new("OfficeSeatEye", None), "OfficeSeatEye")
    eye.location = camera_pos


def shell_chair(x, y, angle):
    # Side profile: depth from the seat front toward the back, height, and half width.
    profile = [(-.212, .440, .170), (-.208, .458, .208), (-.180, .468, .228), (-.110, .462, .238),
               (-.020, .452, .240), (.070, .455, .236), (.135, .470, .224), (.172, .505, .207),
               (.193, .565, .199), (.207, .640, .207), (.219, .725, .222), (.231, .815, .234),
               (.242, .900, .238), (.251, .965, .232), (.257, 1.010, .214), (.260, 1.038, .180),
               (.261, 1.050, .120)]
    columns = 19
    rotation = Matrix.Rotation(angle, 3, "Z")
    verts, uv = [], []
    for row, (depth, height, half) in enumerate(profile):
        back = min(1, max(0, (height - .47) / .12))
        for col in range(columns):
            s = col/(columns-1)*2 - 1
            dish = s*s
            # The painting shows a broad shell; scale the classic proportions up to it.
            local = Vector((s*half*1.26, depth*1.08 - .060*back*dish, .44 + (height-.44)*1.04 + .040*(1-back)*dish))
            verts.append(Vector((x, y, 0)) + rotation @ local)
            uv.append((col/(columns-1), row/(len(profile)-1)))
    faces = [(r*columns + c, r*columns + c + 1, (r+1)*columns + c + 1, (r+1)*columns + c)
             for r in range(len(profile)-1) for c in range(columns-1)]
    shell = mesh("Molded fiberglass shell", verts, faces, fiberglass, uv)
    for poly in shell.data.polygons:
        poly.use_smooth = True
    solid = shell.modifiers.new("Shell thickness", "SOLIDIFY")
    solid.thickness, solid.offset = .0085, 0
    smooth = shell.modifiers.new("Molded curves", "SUBSURF")
    smooth.levels = smooth.render_levels = 1

    def p(dx, dy, z):
        return Vector((x, y, 0)) + rotation @ Vector((dx, dy, z))

    for side in (-1, 1):
        cyl("Shock mount", p(side*.13, -.09, .438), p(side*.13, -.09, .418), .022, rubber, sides=16)
        cyl("Shock mount", p(side*.13, .11, .446), p(side*.13, .11, .426), .022, rubber, sides=16)
        for dy, foot in ((-.09, -.25), (.11, .27)):
            tube("Steel rod leg", [p(side*.13, dy, .420), p(side*.17, (dy+foot)/2, .22), p(side*.215, foot, .025)], .0068, nickel)
            ellipsoid("Nylon glide", p(side*.215, foot, .012), (.014, .014, .010), rubber)
        tube("Side stretcher", [p(side*.165, -.165, .16), p(side*.165, .185, .16)], .0055, nickel)
    tube("Cross stretcher", [p(-.165, .01, .16), p(.165, .01, .16)], .0055, nickel)


def bookcase(x_wall, y0, y1):
    """A low walnut bookcase on the side wall: LPs filed upright, binders, and the week's trades."""
    depth, top = .34, .98
    cx = x_wall + depth/2
    box("Bookcase top", (cx, (y0+y1)/2, top-.012), (depth+.012, y1-y0+.012, .024), desk_wood, .004, grain=(1, 0))
    box("Bookcase back", (x_wall+.006, (y0+y1)/2, top/2), (.012, y1-y0, top), dark_wood, .002)
    for y in (y0+.011, (y0+y1)/2, y1-.011):
        box("Bookcase side", (cx, y, top/2), (depth, .022, top), walnuts[2], .004, grain=(2, 0))
    for z in (.035, .50):
        box("Bookcase shelf", (cx, (y0+y1)/2, z), (depth-.01, y1-y0-.02, .022), walnuts[0], .003, grain=(1, 0))
    box("Bookcase kick", (cx+depth/2-.03, (y0+y1)/2, .03), (.02, y1-y0, .06), dark_wood, .003)
    binder_colors = [material(f"Cloth binder {i + 1}", c, .74) for i, c in enumerate(("2e3a4f", "5a2a22", "3e4a33", "6a5634"))]
    for bay, (start, end) in enumerate(((y0+.022, (y0+y1)/2-.011), ((y0+y1)/2+.011, y1-.022))):
        for shelf_z in (.046, .511):
            y = start + .006
            while y < end - .03:
                if (bay + (shelf_z > .3)) % 2 == 0:
                    thick = prop_rng.uniform(.0035, .0075)
                    box("Filed LP", (cx+.01, y+thick/2, shelf_z+.157), (.315, thick, .314), prop_rng.choice(sleeve_stock), 0)
                    y += thick + .0006
                else:
                    thick = prop_rng.uniform(.045, .075)
                    binder = box("Ring binder", (cx+.025, y+thick/2, shelf_z+.152), (.285, thick, .304),
                                 prop_rng.choice(binder_colors), .004)
                    box("Binder label", (cx+.025+.143, y+thick/2, shelf_z+.19), (.003, thick*.55, .075), paper, 0)
                    y += thick + .004
    for i in range(9):
        box("Trade weekly", (cx+.01+prop_rng.uniform(-.01, .01), y1-.30+prop_rng.uniform(-.01, .01), top+.003+i*.004),
            (.30, .40, .0035), prop_rng.choice(paper_stock), 0, angle=prop_rng.uniform(-.05, .05))


def cork_finish():
    field = TextureField(768, 512, 1907)
    granules = field.pebble(150, 100)
    flecks = field.noise(220, 150)
    tone = .30 + .32*granules + .18*(flecks-.5) + .12*(field.noise(6, 4)-.5)
    return finish("Pressed cork", palette("4e3320", "a8774a", tone), .88 + .06*(flecks-.5), .30*granules, .55)


def cork_board(center):
    """A new label's cork board: who to call, where the jukeboxes are, and a band's handbill."""
    out = Vector((1, 0, 0))
    w, h = .90, .60
    box("Cork board frame", center, (.030, w+.05, h+.05), frame_wood, .004, grain=(1, 2))
    box("Cork face", center + out*.016, (.004, w, h), cork, .001)
    notes = (((-.30, .16), (.130, .085), 0), ((-.13, .19), (.150, .095), 1), ((.06, .15), (.210, .290), 2),
             ((.29, .18), (.125, .080), 0), ((-.29, -.05), (.148, .210), 3), ((-.10, -.09), (.126, .080), 1),
             ((.28, -.07), (.150, .095), 0), ((.30, -.21), (.126, .080), 3), ((-.12, -.22), (.180, .060), 2))
    for (dy, dz), (nw, nh), stock in notes:
        tilt = prop_rng.uniform(-.09, .09)
        spot = center + out*.019 + Vector((0, dy, dz))
        note = box("Pinned note", spot, (.0015, nw, nh), paper_stock[stock], 0)
        note.rotation_euler.x = tilt
        pin = spot + out*.004 + Vector((0, 0, nh*.40))
        ellipsoid("Map pin", pin, (.007, .007, .007), prop_rng.choice(pin_colors))
    # A handbill for a local act: a dark photo block and type bars on the biggest sheet.
    bill = center + out*.0205 + Vector((0, .06, .17))
    box("Handbill photo", bill, (.0006, .15, .095), vinyl, 0)
    for i, width in enumerate((.17, .12, .15, .09)):
        box("Handbill type", bill + Vector((0, 0, -.075 - i*.024)), (.0006, width, .011), enamel, 0)


def wall_clock(center):
    out = Vector((1, 0, 0))
    lathe("Chrome clock case", center, [(.0001, 0), (.150, 0), (.160, .012), (.158, .040), (.146, .046), (.140, .024),
          (.0001, .024)], nickel, sides=64, axis=(1, 0, 0))
    cyl("Clock dial", center + out*.026, center + out*.028, .138, ivory, sides=64)
    for hour in range(12):
        a = hour * math.tau / 12
        mark = center + out*.029 + Vector((0, .118*math.sin(a), .118*math.cos(a)))
        tick = box("Hour mark", mark, (.002, .007, .022 if hour % 3 == 0 else .013), enamel, 0)
        tick.rotation_euler.x = -a
    for length, width, a, name in ((.072, .008, math.radians(-62), "Hour hand"), (.108, .005, math.radians(118), "Minute hand")):
        hand = box(name, center + out*.031 + Vector((0, length/2*math.sin(a), length/2*math.cos(a))),
                   (.002, width, length), enamel, 0)
        hand.rotation_euler.x = -a
    cyl("Clock arbor", center + out*.029, center + out*.034, .007, brass, sides=16)


def filing_cabinet(cx, y_wall):
    """A four-drawer steel file in the corner behind the desk."""
    w, d, h = .38, .66, 1.33
    cy = y_wall + d/2
    box("File cabinet case", (cx, cy, h/2+.01), (w, d, h), file_steel, .010)
    for i in range(4):
        z = .045 + (i+.5)*(h-.07)/4
        front = cy + d/2 + .009
        box("File drawer front", (cx, front, z), (w-.025, .018, (h-.07)/4-.016), file_steel, .007)
        box("Label holder", (cx, front+.011, z+.085), (.085, .004, .038), nickel, .002)
        box("File label", (cx, front+.0125, z+.085), (.070, .002, .026), paper, 0)
        cyl("Thumb latch handle", (cx-.06, front+.024, z+.035), (cx+.06, front+.024, z+.035), .009, nickel, sides=16)
        for dx in (-.06, .06):
            cyl("Handle boss", (cx+dx, front+.009, z+.035), (cx+dx, front+.026, z+.035), .007, nickel, sides=12)
    for i in range(6):
        box("Manila folder", (cx+prop_rng.uniform(-.01, .01), cy+prop_rng.uniform(-.02, .02), h+.016+i*.005),
            (.24, .31, .004), manila, 0, angle=prop_rng.uniform(-.08, .08))


def coat_tree(base):
    """A bentwood coat tree by the door, the founder's hat on a hook."""
    x, y = base
    lathe("Turned coat tree pole", (x, y, 0), [(.0001, .02), (.020, .02), (.018, .30), (.016, 1.20), (.019, 1.66),
          (.026, 1.70), (.018, 1.76), (.0001, 1.79)], dark_wood, sides=24)
    for i in range(4):
        a = i * math.tau/4 + .4
        tube("Bentwood foot", [(x+.02*math.cos(a), y+.02*math.sin(a), .40), (x+.14*math.cos(a), y+.14*math.sin(a), .16),
             (x+.27*math.cos(a), y+.27*math.sin(a), .025)], .013, dark_wood)
    for i in range(6):
        a = i * math.tau/6
        z = 1.58 if i % 2 else 1.50
        tube("Bentwood hook", [(x+.015*math.cos(a), y+.015*math.sin(a), z), (x+.12*math.cos(a), y+.12*math.sin(a), z+.05),
             (x+.16*math.cos(a), y+.16*math.sin(a), z+.12)], .0085, dark_wood)
    hat = Vector((x+.15*math.cos(math.tau/6), y+.15*math.sin(math.tau/6), 1.66))
    axis = Vector((.35, .1, 1)).normalized()
    lathe("Felt fedora brim", hat, [(.075, -.002), (.150, .006), (.170, .018), (.168, .024), (.148, .014), (.075, .008)],
          felt, sides=40, axis=axis)
    lathe("Felt fedora crown", hat, [(.090, .005), (.094, .060), (.086, .110), (.060, .128), (.0001, .118)],
          felt, sides=40, axis=axis)
    lathe("Grosgrain hat band", hat, [(.0935, .010), (.0950, .012), (.0950, .043), (.0935, .045)], hat_band, sides=40, axis=axis)


def radiator(cx, y_wall):
    """A cast-iron column radiator under the window, painted with aluminium bronze."""
    sections = 14
    for i in range(sections):
        x = cx + (i - (sections-1)/2) * .062
        for dy in (.045, .105):
            box("Radiator column", (x, y_wall+dy, .40), (.040, .042, .56), radiator_paint, .016)
    for z in (.135, .665):
        cyl("Radiator header", (cx-.47, y_wall+.075, z), (cx+.47, y_wall+.075, z), .026, radiator_paint)
    cyl("Radiator valve", (cx+.49, y_wall+.075, .18), (cx+.56, y_wall+.075, .18), .022, brass)
    for foot in (-.40, .40):
        box("Radiator foot", (cx+foot, y_wall+.075, .07), (.05, .12, .14), radiator_paint, .01)


def intro_set(door_left, door_right):
    group("OfficeIntroSet")
    plaster = material("Tobacco-stained ceiling plaster", "635647", .89)
    hallway = material("Dim hallway paint", "393a34", .89)
    far_wall = -.77
    for x in (-3.54, 1.38):
        for i in range(15):
            y = far_wall + (i+.5)*.24
            box("Side-wall veneer", (x, y, 1.30), (.064, .237, 2.60), walnuts[i % len(walnuts)], .002, grain=(2, 1))
        box("Side-wall baseboard", (x, 1.02, .053), (.086, 3.55, .105), dark_wood, .009)
    for i in range(21):
        x = -3.53 + (i+.5)*.235
        box("Opposite wall veneer", (x, far_wall, 1.30), (.232, .068, 2.60),
            walnuts[i % len(walnuts)], .002, grain=(2, 0))
    box("Opposite wall baseboard", (-1.06, far_wall+.033, .053), (4.93, .032, .105), dark_wood, .006)
    box("Acoustic tile ceiling", (-1.06, 1.02, 2.61), (5, 3.65, .07), ceiling_tile, .006, grain=(0, 1),
        grain_scale=(1.2192, 1.2192))
    for y in (far_wall+.045, 2.79-.045):
        box("Ceiling molding", (-1.06, y, 2.555), (4.93, .04, .05), dark_wood, .008)

    # A school-house globe on a brass fitter, lit for the walk in.
    fitter = Vector((-1.06, 1.02, 2.575))
    lathe("Brass ceiling fitter", fitter, [(.0001, 0), (.075, 0), (.078, -.010), (.060, -.040), (.052, -.046),
          (.0001, -.046)], brass, sides=48)
    lathe("Milk glass globe", fitter, [(.050, -.044), (.078, -.075), (.105, -.140), (.110, -.190), (.095, -.240),
          (.050, -.272), (.0001, -.278)], milk_glass, sides=64)

    # The window: glass full of daylight behind half-open aluminium blinds.
    window_x = -1.17
    box("Window casing", (window_x, far_wall+.049, 1.79), (1.24, .045, .80), frame_wood, .022, grain=(0, 2))
    box("Daylit window glass", (window_x, far_wall+.073, 1.79), (1.12, .004, .68), daylight_glass, .001)
    for dz in (-.002,):
        box("Meeting rail", (window_x, far_wall+.080, 1.79+dz), (1.12, .012, .035), frame_wood, .003)
    for dx in (-.56, .56):
        box("Window stop", (window_x+dx, far_wall+.088, 1.79), (.030, .022, .68), frame_wood, .003, grain=(2, 0))
    box("Blind head rail", (window_x, far_wall+.108, 2.140), (1.10, .040, .038), blind_paint, .006)
    for i in range(27):
        slat = box("Aluminium blind slat", (window_x, far_wall+.108, 1.475+i*.0245), (1.08, .026, .0022), blind_paint, 0)
        slat.rotation_euler.x = -.62
    box("Blind bottom rail", (window_x, far_wall+.108, 1.452), (1.08, .030, .014), blind_paint, .004)
    for dx in (-.38, .38):
        for dy in (-.014, .014):
            tube("Ladder tape", [(window_x+dx, far_wall+.108+dy, 2.125), (window_x+dx, far_wall+.108+dy, 1.452)], .0012, blind_tape)
    tube("Lift cord", [(window_x+.50, far_wall+.122, 2.125), (window_x+.50, far_wall+.122, 1.20)], .0013, blind_tape)
    ellipsoid("Cord tassel", (window_x+.50, far_wall+.122, 1.185), (.009, .009, .022), ivory)
    box("Window sill", (window_x, far_wall+.092, 1.40), (1.29, .19, .056), frame_wood, .009, grain=(0, 1))
    radiator(window_x, far_wall+.034)

    # The walk in passes the side wall and the corner behind the desk; furnish them.
    side = -3.54 + .032
    bookcase(side, .45, 2.05)
    cork_board(Vector((side+.015, 1.05, 1.50)))
    wall_clock(Vector((side, 1.78, 1.70)))
    filing_cabinet(-3.25, far_wall+.034)
    coat_tree((-3.12, 2.47))
    lathe("Steel waste basket", (-1.98, .02, 0), [(.0001, .004), (.115, .004), (.140, .33), (.146, .335), (.142, .34),
          (.134, .012), (.0001, .012)], file_steel, sides=48)
    for i in range(5):
        a = prop_rng.uniform(0, math.tau)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=.045,
                                              location=(-1.98+.06*math.cos(a), .02+.06*math.sin(a), .28+i*.025))
        ball = link(bpy.context.object, "Crumpled demo sheet", prop_rng.choice(paper_stock))
        for vertex in ball.data.vertices:
            vertex.co *= prop_rng.uniform(.72, 1.18)

    hall_x = (door_left+door_right)/2
    width = door_right-door_left+.40
    for x in (hall_x-width/2, hall_x+width/2):
        box("Hallway wall", (x, 3.62, 1.23), (.075, 1.68, 2.46), hallway, .003)
        box("Hallway baseboard", (x, 3.61, .058), (.093, 1.64, .12), dark_wood, .006)
    box("Hallway end wall", (hall_x, 4.45, 1.22), (width, .08, 2.44), hallway, .003)
    box("Hallway floor", (hall_x, 3.70, -.068), (width, 1.58, .10), dark_wood, .003)
    box("Hallway ceiling", (hall_x, 3.60, 2.44), (width, 1.66, .065), plaster, .003)


def desk_lamp():
    group("Articulated desk lamp")
    base = at_z(122, 470, DESK)
    lathe("Weighted walnut lamp base", base, [(.0001, 0), (.145, 0), (.158, .006), (.159, .028),
          (.147, .039), (.0001, .039)], walnuts[0], y_scale=.71)
    bottom = at_y(160, 437, base.y)
    elbow = at_y(93, 181, base.y + .025)
    hinge = at_y(86, 163, base.y + .020)
    throat = at_y(107, 51, base.y + .034)
    arm = box("Walnut articulated lower arm", (bottom + elbow) / 2, (.080, .044, (elbow-bottom).length), desk_wood, .007, grain=(2, 0))
    arm.rotation_euler = (elbow - bottom).to_track_quat("Z", "Y").to_euler()
    cyl("Brass lower hinge stem", elbow, hinge, .014, brass)
    cyl("Brass upper arm", hinge, throat, .011, brass)
    for v in (bottom, hinge, throat):
        cyl("Slotted brass pivot", v + Vector((-.034, 0, 0)), v + Vector((.034, 0, 0)), .022, brass)
    shade = at_y(241, 49, 1.59)
    axis = Vector((-.045, .055, 1)).normalized()
    outer = [(.307, 0), (.305, .016), (.288, .052), (.255, .105), (.211, .155), (.160, .189), (.037, .195)]
    lathe("Black enamel shade exterior", shade, outer, enamel, axis=axis)
    lathe("Warm enamel shade interior", shade, [(r - .004, z) for r, z in reversed(outer)], shade_lining, axis=axis)
    lathe("Rolled brass shade rim", shade, [(.300, -.002), (.306, -.004), (.309, .000), (.307, .005), (.302, .005)], brass, axis=axis)
    bulb = shade + Vector((-.015, .026, .040))
    ellipsoid("Frosted tungsten bulb", bulb, (.059, .051, .056), bulb_mat)
    anchor = link(bpy.data.objects.new("Lamp light position", None), "Lamp light position")
    anchor.location = shade + Vector((0, -.025, -.038))


def frame():
    group("Framed trade clipping")
    quad = [at_y(*p, WALL - .057) for p in ((980, 36), (1333, 36), (1326, 238), (976, 228))]
    panel("Frame back", quad, .028, walnuts[0], dark_wood, .003)
    for i in range(4):
        a, b = quad[i], quad[(i + 1) % 4]
        frame_rail = box("Mitered walnut rail", (a+b)/2 + Vector((0, -.016, 0)),
                         (.017, .025, (b-a).length + .010), frame_wood, .003, grain=(2, 0))
        frame_rail.rotation_euler = (b-a).to_track_quat("Z", "Y").to_euler()
    print_coords = ((989, 47), (1321, 47), (1316, 225), (985, 217))
    art = artwork("Aged newsprint and framed record", print_coords, 768, 400)
    panel("Printed clipping and mat", [at_y(*p, WALL - .064) for p in print_coords], .003, art)


def records():
    group("Left record sleeves")
    # Register the top sleeve and disc against the UI's catalogue overlays.
    a, b, d = at_y(792, 289, 2.65), at_y(977, 296, 2.65), at_y(730, 309, 2.25)
    normal = (d - a).cross(b - a).normalized()
    lid = (a, b, at_plane(932, 318, a, normal), d)
    for i in range(19):
        height = (i + 1) / 20
        offset = Vector((rng.uniform(-.012, .012), rng.uniform(-.012, .012), 0))
        sleeve = [Vector((v.x, v.y, CREDENZA + (v.z-CREDENZA)*height)) + offset for v in lid]
        panel("Record sleeve", sleeve, .0055, rng.choice(sleeve_stock), vinyl, .001)
    panel("Top sleeve", lid, .006, sleeve_stock[2], sleeve_stock[3], .001)
    center = at_plane(991 * WIDTH / 1920, 347 * HEIGHT / 1080, a, normal)
    for label, radius, z, mat in (("Vinyl disc", .129, .001, vinyl), ("Paper centre label", .032, .004, record_label),
                                  ("Spindle hole", .0028, .005, vinyl)):
        cyl(label, center + normal*z, center + normal*(z+.0013), radius, mat, sides=64)
    for r in (.068, .090, .103, .119):
        lathe("Pressed vinyl grooves", center, [(r, .0024), (r+.0006, .00245)], groove, sides=64, axis=normal)

    group("Right record sleeves")
    origin = at_z(1601, 412, CREDENZA)
    for i in range(29):
        x = origin.x + rng.uniform(-.014, .014)
        y = origin.y + rng.uniform(-.020, .020)
        box("Record sleeve", (x, y, CREDENZA + .003 + i*.0067), (.34, .32, .0056),
            rng.choice(sleeve_stock), .0013, angle=rng.uniform(-.055, .055))


def typewriter():
    group("Olive mechanical typewriter")
    p = at_z(1132, 385, CREDENZA)
    x, y = p.x, p.y
    box("Typewriter foot", (x, y, CREDENZA+.016), (.425, .325, .032), rubber, .012)
    box("Cast base shell", (x, y, CREDENZA+.039), (.473, .353, .067), enamel_olive, .022, smooth=True)
    box("Carriage housing", (x, y+.088, CREDENZA+.124), (.442, .177, .129), enamel_olive, .021, smooth=True)
    keyboard = box("Recessed keyboard tray", (x, y-.081, CREDENZA+.072), (.411, .167, .022), enamel, .009)
    keyboard.rotation_euler.x = .20
    for direction in (-1, 1):
        cap = box("Sculpted keyboard side wall", (x + direction*.220, y-.064, CREDENZA+.082),
                  (.031, .202, .049), enamel_olive, .012)
        cap.rotation_euler.x = .26
    row_text = ("1234567890-=", "QWERTYUIOP", "ASDFGHJKL;", "ZXCVBNM,.")
    for row, keys in enumerate(row_text):
        for col, key in enumerate(keys):
            px = x + (col - (len(keys)-1)/2) * .031
            py = y + .009 - row * .037
            pz = CREDENZA + .123 - row * .011
            cyl("Typewriter key linkage", (px, py, pz-.013), (px, py, pz), .006, gunmetal, sides=10)
            cap = box(f"Key {key}", (px, py, pz+.001), (.025, .024, .011), ivory, .003)
            cap.rotation_euler.x = .10
    box("Space bar", (x, y-.150, CREDENZA+.082), (.236, .023, .013), ivory, .004)
    box("Ribbon recess", (x, y+.064, CREDENZA+.190), (.182, .075, .003), rubber, .003)
    for dx in (-.070, .070):
        cyl("Ribbon spool", (x+dx, y+.064, CREDENZA+.191), (x+dx, y+.064, CREDENZA+.196), .031, gunmetal)
    cyl("Rubber platen", (x-.224, y+.146, CREDENZA+.216), (x+.224, y+.146, CREDENZA+.216), .021, rubber)
    cyl("Carriage paper guide", (x-.244, y+.104, CREDENZA+.216), (x+.244, y+.104, CREDENZA+.216), .004, nickel)
    for dx in (-1, 1):
        cyl("Platen knob", (x+dx*.225, y+.146, CREDENZA+.216), (x+dx*.261, y+.146, CREDENZA+.216), .023, gunmetal)
        cyl("Platen knob inset", (x+dx*.260, y+.146, CREDENZA+.216), (x+dx*.269, y+.146, CREDENZA+.216), .013, brass)
    for dx in (-.17, .17):
        box("Paper bail", (x+dx, y+.104, CREDENZA+.216), (.032, .014, .009), rubber, .002)
    sheet = [Vector((x+dx, y+dy, CREDENZA+dz)) for dx, dy, dz in
             ((-.148, .173, .275), (.148, .173, .275), (.148, .119, .207), (-.148, .119, .207))]
    panel("Sheet in the platen", sheet, .0005, paper)
    tube("Carriage return lever", [(x-.209, y+.146, CREDENZA+.227), (x-.251, y+.122, CREDENZA+.231),
         (x-.272, y+.080, CREDENZA+.214), (x-.300, y+.050, CREDENZA+.214)], .005, nickel)


def phone():
    group("Honey Bakelite rotary telephone")
    x, y, _ = at_z(1394, 383, CREDENZA)
    rotation = Quaternion((0, 0, 1), -.10)

    def p(dx, dy, dz):
        return Vector((x, y, CREDENZA)) + rotation @ Vector((dx*.92, dy, dz))

    levels = (
        ((-.148, -.119), (.148, -.119), (.146, .115), (-.146, .115)),
        ((-.145, -.115), (.145, -.115), (.140, .111), (-.140, .111)),
        ((-.128, -.099), (.128, -.099), (.119, .086), (-.119, .086)),
    )
    points = [p(dx, dy, h) for corners, h in zip(levels, (.004, .022, .082)) for dx, dy in corners]
    points[8].z -= .037
    points[9].z -= .037
    points[10].z += .034
    points[11].z += .034
    chassis = mesh("Faceted Bakelite chassis", points, [(3, 2, 1, 0), (8, 9, 10, 11)] +
                   [(i+j, (i+1) % 4+j, (i+1) % 4+j+4, i+j+4) for j in (0, 4) for i in range(4)], bakelite,
                   [(dx/.296+.5, dy/.238+.5) for corners in levels for dx, dy in corners])
    bevel(chassis, .017, 5)
    normal = rotation @ Vector((0, -.36, .933)).normalized()
    center = p(0, -.027, .076)
    cyl("Rotary dial chrome bezel", center, center + normal*.005, .072, nickel)
    cyl("Number plate", center + normal*.004, center + normal*.008, .066, ivory)
    u, v = rotation @ Vector((1, 0, 0)), normal.cross(rotation @ Vector((1, 0, 0)))
    for i in range(10):
        a = math.radians(i * 29 + 37)
        pos = center + u * (.052*math.cos(a)) + v * (.052*math.sin(a)) + normal*.0086
        cyl("Rotary finger hole recess", pos, pos + normal*.003, .0092, gunmetal, sides=20)
        lathe("Finger wheel rim", pos, [(.0092, .0026), (.0113, .0026), (.0113, .0038), (.0092, .0038)],
              tarnished_brass, sides=20, axis=normal)
    cyl("Rotary hub", center + normal*.007, center + normal*.014, .034, tarnished_brass)
    cyl("Number card", center + normal*.014, center + normal*.015, .027, ivory)
    box("Finger stop", p(.053, -.095, .072), (.025, .009, .009), nickel, .002, angle=-.10)
    for dx in (-.091, .091):
        box("Cradle horn", p(dx, .058, .097), (.036, .049, .052), bakelite, .010, angle=-.10)
    handset_profile = ((-.154, .027, .130, .041, .033), (-.127, .041, .145, .040, .030),
                       (-.097, .055, .161, .029, .021), (-.055, .071, .172, .021, .013),
                       (0, .074, .174, .020, .013), (.055, .070, .168, .022, .014),
                       (.100, .053, .154, .032, .024), (.137, .031, .138, .041, .033),
                       (.157, .018, .123, .039, .026))
    handset = []
    for dx, dy, dz, ry, rz in handset_profile:
        for i in range(24):
            a = i * math.tau / 24
            handset.append(p(dx, dy + ry*math.cos(a), dz + rz*math.sin(a)))
    faces = [tuple(range(23, -1, -1)), tuple(range(len(handset)-24, len(handset)))]
    faces += [(j*24+i, j*24+(i+1)%24, (j+1)*24+(i+1)%24, (j+1)*24+i)
              for j in range(len(handset_profile)-1) for i in range(24)]
    receiver = mesh(PHONE_RECEIVER, handset, faces, bakelite)
    ring_uv(receiver, 24, [(row[0]+.154)/.311 for row in handset_profile], caps=2)
    bevel(receiver, .008)
    for poly in receiver.data.polygons:
        poly.use_smooth = True
    cord = []
    for i in range(241):
        t = i/240
        a = t * math.tau * 16
        cord.append(p(-.143 - .100*math.sin(math.pi*t) + .010*math.cos(a), .006 - .129*t + .009*math.sin(a),
                      .020 + .107*(1-t)**3 + .007*math.cos(a)))
    tube("Coiled handset cable", cord, .0038, cord_mat)


def paperwork():
    group("Trade papers")
    top = projected_quad(((117, 450), (459, 441), (339, 559), (-26, 550)), DESK + .052)
    for i in range(15):
        offset = Vector((rng.uniform(-.018, .018), rng.uniform(-.022, .014), -.049 + i*.0032))
        panel("Loose folded trade sheet", [v + offset for v in top], .0029, rng.choice(paper_stock), paper_edge, .001)
    print_coords = ((119, 451), (455, 443), (339, 554), (1, 547))
    panel("Trade masthead", top, .0012, artwork("Record trade front page", print_coords, 900, 620))

    group("Ruled A&R desk pad")
    pad = projected_quad(((605, 547), (1121, 582), (1107, 1070), (85, 901)), DESK + .025)
    panel("Pad backing", [v - Vector((0, 0, .006)) for v in pad], .021, dark_wood, radius=.004)
    field = TextureField(1536, 1024, 164)
    ledger = material("Printed ruled ledger stock", "d4b98d",
                      maps=paper_finish("Ruled ledger paper", field, np.array(color("d4b98d")), ruled=True))
    stack = panel("Stack of ruled paper", pad, .022, ledger, paper_edge, .002)
    # Project the print across the plane so the rules stay straight through triangulation.
    a, b, c, _ = pad
    across, down = b-a, c-b
    area_scale = across.cross(down).z
    layer = stack.data.uv_layers.active
    for i in stack.data.polygons[0].loop_indices:
        offset = stack.data.vertices[stack.data.loops[i].vertex_index].co - a
        layer.data[i].uv = (offset.cross(down).z/area_scale, 1-across.cross(offset).z/area_scale)

    group("Leather checkbook and cheques")
    left = projected_quad(((369, 529), (601, 564), (290, 773), (14, 713)), DESK + .048)
    right_quad = projected_quad(((603, 561), (843, 605), (714, 814), (278, 773)), DESK + .048)
    for name, cover in (("Grained leather flap", left), ("Leather checkbook support", right_quad)):
        panel(name, cover, .017, leather, edge_leather, .006, rounded=.019)
        stitch_height = Vector((0, 0, .0012))
        for a, b in [((.04, .037), (.96, .037)), ((.96, .037), (.96, .963)),
                     ((.96, .963), (.04, .963)), ((.04, .963), (.04, .037))]:
            v1, v2 = interpolate_quad(cover, *a), interpolate_quad(cover, *b)
            count = int((v1-v2).length / .010)
            for i in range(count):
                tube("Saddle stitch", [v1.lerp(v2, t) + stitch_height for t in (i/count, (i+.58)/count)], .00068, thread)
    for i in range(4):
        panel("Tucked payment slip", projected_quad(((450, 587), (591, 569), (391, 701), (295, 723)),
              DESK+.050+i*.002), .002, rng.choice(paper_stock), paper_edge, .001)
    check = projected_quad(((610, 571), (803, 565), (642, 758), (369, 748)), DESK + .0856)
    for i in range(14):
        offset = Vector((rng.uniform(-.003, .003), rng.uniform(-.004, .004), -.034 + i*.0024))
        panel("Cheque in the register", [v + offset for v in check], .0021, paper_stock[i % 3], paper_edge)
    panel("Printed cheque face", check, .0014,
          artwork("Cheque printing", ((612, 576), (788, 568), (639, 747), (390, 738)), 768, 395), paper_edge, .001)


def calendar():
    group("Three-ring flip calendar")
    a = at_z(1107, 550, DESK + .004)
    b = at_z(1547, 614, DESK + .004)
    c = at_y(1559, 448, b.y + .106)
    normal = (b - a).cross(c - b).normalized()

    def on_front(u, v, offset=0):
        return at_plane(u, v, a + normal*offset, normal)

    face = [on_front(*p) for p in ((1133, 412), (1559, 448), (1547, 614), (1107, 550))]
    panel("Brushed steel face", face, .010, gunmetal, nickel, .0035)
    rear = [face[0], face[1], b + Vector((0, .19, 0)), a + Vector((0, .19, 0))]
    panel("Folded steel stand", rear, .009, gunmetal, gunmetal, .003)
    for corners, pin in (
            (((1143, 428), (1253, 440), (1237, 533), (1126, 517)), (1200, 444)),
            (((1268, 440), (1380, 451), (1365, 552), (1250, 536)), (1322, 456)),
            (((1398, 455), (1529, 467), (1514, 569), (1380, 552)), (1459, 467))):
        for i in range(4):
            panel("Calendar index card", [on_front(*p, .004+i*.002) for p in corners],
                  .002, paper, paper_edge, .0008, rounded=.08)
        pin_location = on_front(*pin, .010)
        cyl("Ring hole", pin_location, pin_location + normal*.002, .008, gunmetal)
        ring = [pin_location + Vector((0, .021 + .025 * math.cos(math.radians(a)), .025 + .035 * math.sin(math.radians(a))))
                for a in range(-127, 233, 8)]
        tube("Bent nickel binding ring", ring, .0044, nickel)


def ashtray():
    global collection
    group("Molded glass ashtray")
    center = at_z(1429, 787, DESK)
    profile = [(0.001, .001), (.135, .001), (.152, .008), (.161, .022), (.163, .044), (.162, .056),
               (.157, .063), (.150, .064), (.144, .058), (.141, .042), (.134, .025), (.126, .017), (.001, .017)]
    lathe("Heavy smoked glass bowl", center, [(r*1.105, h) for r, h in profile], glass, 112, 1.04, notch=.014)
    for radius, h in ((.155, .018), (.161, .033), (.149, .061)):
        radius *= 1.105
        lathe("Pressed rim highlight", center, [(radius-.001, h-.002), (radius+.001, h), (radius-.0008, h+.0018)], glass, 112, 1.04, notch=.014)
    for i in range(165):
        # Model seven piles of two cigarettes (the final pile has one). Godot reveals them in order.
        if i % 24 == 0:
            group(f"OfficeAshtrayWaste{i // 24 + 1}")
        a, r = rng.uniform(0, math.tau), math.sqrt(rng.random()) * .138
        pos = center + Vector((math.cos(a)*r, math.sin(a)*r*.98, .021 + rng.uniform(0, .009)))
        rad = rng.uniform(.0025, .0070)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=rad, location=pos)
        chip = link(bpy.context.object, "Irregular ash fragment", rng.choice(ash_colors))
        chip.scale = (1, rng.uniform(.4, 1.2), .62)
        chip.rotation_euler.z = a
    for i in range(13):
        collection = bpy.data.collections[f"OfficeAshtrayWaste{i // 2 + 1}"]
        ang = rng.uniform(0, math.tau)
        a = center + Vector((rng.uniform(-.079, .079), rng.uniform(-.077, .077), rng.uniform(.026, .040)))
        b = a + Vector((math.cos(ang)*.053, math.sin(ang)*.053, rng.uniform(-.006, .018)))
        if i == 0:
            # Camera-space origin used by DeskAtmosphere's animated smoke.
            b = at_z(1664 * WIDTH / 1920, 764 * HEIGHT / 1080, DESK + .052)
            a = b + Vector((.031, -.048, -.020))
        split = a.lerp(b, rng.uniform(.38, .59))
        end = a.lerp(b, .92)
        cyl("Crushed cork filter", a, split, .0066, filter_mat, sides=16)
        cyl("Crumpled cigarette paper", split, end, .0067, ivory, r2=.0056, sides=16)
        cyl("Burned cigarette tip", end, b, .0057, ash_colors[1], r2=.0043, sides=14)

    resize = Matrix.Translation(center) @ Matrix.Scale(.75, 4) @ Matrix.Translation(-center)
    for name in ["Molded glass ashtray", *(f"OfficeAshtrayWaste{i}" for i in range(1, 8))]:
        for obj in bpy.data.collections[name].objects:
            obj.matrix_world = resize @ obj.matrix_world


def lighting():
    group("Blender preview lights")
    lamp = bpy.data.objects["Lamp light position"].location
    area("Warm desk lamp", lamp, (-.67, 1.30, DESK), 49, (1, .69, .34), .32)
    area("Soft window light", (-2.42, .45, 2.45), (.1, 2.1, .99), 82, (1, .88, .72), 1.70)
    area("Office bounce", (1.80, .21, 1.92), (-.3, 2, .88), 47, (.76, .83, 1), 2.45)
    world = bpy.data.worlds.new("Warm office ambient")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (.25, .18, .12, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = .19
    scene.world = world


def export_scene(out):
    bpy.context.preferences.filepaths.save_version = 0
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 36
    scene.cycles.use_denoising = True
    scene.render.resolution_x, scene.render.resolution_y = WIDTH, HEIGHT
    scene.render.resolution_percentage = 100
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "AgX - Medium High Contrast"
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                area.spaces.active.region_3d.view_perspective = "CAMERA"
    bpy.ops.wm.save_as_mainfile(filepath=str(out / "Label Man Office.blend"))

    # Keep the glass material transmissive in Cycles, with a translucent approximation in Godot GL.
    shader = glass.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Transmission Weight"].default_value = 0
    shader.inputs["Alpha"].default_value = .45
    glass.surface_render_method = "BLENDED"
    # Batch static parts of each prop, retaining UVs, material slots and evaluated normals.
    depsgraph = bpy.context.evaluated_depsgraph_get()
    bpy.ops.object.select_all(action="DESELECT")
    for coll in list(scene.collection.children):
        objects = [o for o in coll.objects if o.type in {"MESH", "CURVE"}]
        exports = []
        for obj in objects:
            evaluated = obj.evaluated_get(depsgraph)
            data = bpy.data.meshes.new_from_object(evaluated, preserve_all_data_layers=True, depsgraph=depsgraph)
            export = bpy.data.objects.new(obj.name, data)
            coll.objects.link(export)
            export.matrix_world = obj.matrix_world
            if obj.name == PHONE_RECEIVER:
                export.name = "OfficePhoneReceiver"
            else:
                exports.append(export)
        for obj in exports:
            obj.select_set(True)
        if exports:
            bpy.context.view_layer.objects.active = exports[0]
            if len(exports) > 1:
                bpy.ops.object.join()
            exports[0].name = coll.name
            exports[0].select_set(False)
            hinge = coll.objects.get("OfficeDoorHinge")
            if hinge:
                world = exports[0].matrix_world.copy()
                exports[0].parent = hinge
                exports[0].matrix_world = world
        for obj in objects:
            bpy.data.objects.remove(obj, do_unlink=True)
    for obj in scene.objects:
        if obj.type in {"MESH", "CAMERA", "EMPTY"}:
            obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(out / "office.glb"), use_selection=True, export_cameras=True,
                             export_lights=False, export_yup=True, export_unused_images=False)


def build(reference_path, output):
    global scene, reference, camera_pos, forward, right, up
    global walnuts, desk_wood, frame_wood, dark_wood, gunmetal, nickel, brass, tarnished_brass, enamel, enamel_olive
    global rubber, shade_lining, bulb_mat, bakelite, cord_mat, paper, paper_edge, paper_stock
    global leather, edge_leather, thread, vinyl, groove, record_label, ivory, glass, ash_colors, filter_mat
    global floor_tile, rug, ceiling_tile, fiberglass, cork, pin_colors, file_steel, manila, felt, hat_band
    global radiator_paint, milk_glass, daylight_glass, blind_paint, blind_tape, sleeve_stock
    bpy.ops.wm.read_homefile(use_empty=True, use_factory_startup=True)
    rng.seed(61463)
    prop_rng.seed(1960)
    scene = bpy.context.scene
    source = bpy.data.images.load(str(reference_path), check_existing=False)
    if tuple(source.size) != (WIDTH, HEIGHT):
        raise ValueError(f"Reference must be {WIDTH} x {HEIGHT} pixels, got {tuple(source.size)}")
    # Read the original byte values for rectifying the artwork.
    source.colorspace_settings.name = "Non-Color"
    pixels = np.empty(WIDTH * HEIGHT * 4, dtype=np.float32)
    source.pixels.foreach_get(pixels)
    reference = pixels.reshape(HEIGHT, WIDTH, 4)[..., :3]
    bpy.data.images.remove(source)

    wall_maps = walnut_finish("Quarter-sawn walnut", 1536, 768, 813, "25170e", "60402a")
    desk_maps = walnut_finish("Oiled figured walnut", 1536, 1024, 629, "2f190d", "7d5234", figured=True)
    walnuts = [material(f"Walnut panel {i + 1}", "4c3020", maps=wall_maps) for i in range(3)]
    desk_wood = material("Oiled desk and credenza", "5c3920", maps=desk_maps)
    frame_wood = material("Waxed walnut molding", "56331f", maps=wall_maps)
    dark_wood = material("Dark stained end grain", "36251d", .58)
    leather = material("Worn chestnut leather", "5c321e",
                       maps=leather_finish("Chestnut leather with rubbed edges", "2d150c", "774225", 996, worn=True))
    leather.node_tree.nodes.get("Principled BSDF").inputs["Specular IOR Level"].default_value = .10
    edge_leather = material("Dark dyed leather edge", "3d2218", .43)
    thread = material("Waxed linen stitching", "b8925e", .64)
    gunmetal = material("Brushed dark steel", "555755", metal=.55,
                        maps=hardware_finish("Pitted brushed steel", "313431", "74746a", .43, 72, brush=True, oxidized=True))
    nickel = material("Polished aged nickel", "9a907d", metal=.74,
                      maps=hardware_finish("Brushed nickel", "605c52", "aca897", .255, 58, brush=True))
    brass = material("Warm polished brass", "b58a44", metal=.70,
                     maps=hardware_finish("Burnished brass", "705133", "ca9d54", .29, 85, brush=True, oxidized=True))
    tarnished_brass = material("Oxidized brass", "77634b", metal=.54,
                               maps=hardware_finish("Dark brass patina", "493924", "9e8053", .44, 517, oxidized=True))
    enamel = material("Black-green enamel", "282923", metal=.24,
                      maps=hardware_finish("Black enamel with handling scratches", "20231e", "36362f", .34, 218))
    shade_lining = material("Ivory shade enamel", "f2dcad", .38)
    lining_bsdf = shade_lining.node_tree.nodes.get("Principled BSDF")
    lining_bsdf.inputs["Emission Color"].default_value = (*color("f2cb7f"), 1)
    lining_bsdf.inputs["Emission Strength"].default_value = .19
    bulb_mat = material("Tungsten bulb glass", "fff0c5", .20)
    lamp_bsdf = bulb_mat.node_tree.nodes.get("Principled BSDF")
    lamp_bsdf.inputs["Emission Color"].default_value = (*color("ffe4a0"), 1)
    lamp_bsdf.inputs["Emission Strength"].default_value = 5.5
    fiberglass = material("Olive fiberglass shell", "556040", maps=fiberglass_finish())
    rubber = material("Black rubber", "26251e", .66)
    enamel_olive = material("Cast olive typewriter enamel", "797952", metal=.18,
                            maps=hardware_finish("Cast olive enamel", "5b5b3b", "999570", .36, 324, oxidized=True))
    bakelite = material("Honey Bakelite", "b48a54", maps=bakelite_finish())
    cord_mat = material("Golden brown handset cable", "775734", .42)
    paper = material("Cream writing stock", "dfc69a",
                     maps=paper_finish("Cream rag paper", TextureField(512, 512, 819), np.array(color("dfc69a"))))
    paper_edge = material("Aged paper edge", "9c8061", maps=cut_paper_finish())
    paper_stock = [material(f"Paper stock {i + 1}", c, .77) for i, c in enumerate(("c8b58d", "dbbf93", "b6a17b", "decba5"))]
    record_label = material("Faded paper record label", "bd9b64", .73)
    vinyl = material("Black shellac", "24221e", .28)
    groove = material("Shellac groove edge", "3c3932", .33)
    ivory = material("Nicotine-aged ivory", "d2bc8f", .41)
    glass = material("Smoked molded glass", "827261",
                     maps=hardware_finish("Scuffed molded glass", "716252", "95826e", .128, 432))
    glass_bsdf = glass.node_tree.nodes.get("Principled BSDF")
    glass_bsdf.inputs["Transmission Weight"].default_value = 1
    glass_bsdf.inputs["IOR"].default_value = 1.50
    ash_colors = [material(f"Ash and charcoal {i + 1}", c, .90) for i, c in enumerate(("706760", "392e24", "988b79", "b4a895"))]
    filter_mat = material("Cork cigarette filter", "9c622c",
                          maps=hardware_finish("Flecked cork paper", "70441f", "b57a42", .73, 32, oxidized=True))

    floor_tile = material("Marbled vinyl floor tile", "6a6048", maps=tile_floor_finish())
    rug = material("Madder wool rug", "6a2a1f", maps=rug_finish())
    ceiling_tile = material("Acoustic ceiling tile", "d3c8ae", maps=ceiling_tile_finish())
    file_steel = material("Office green steel", "6b7262", metal=.20,
                          maps=painted_steel_finish("Office green enamel", "4f5649", "8a917c", 1955))
    radiator_paint = material("Aluminium-bronze radiator paint", "8d8470", metal=.55,
                              maps=hardware_finish("Radiator bronze paint", "5d5646", "a59a80", .52, 1922, oxidized=True))
    blind_paint = material("Cream enamelled aluminium", "d8cdb0", .36, metal=.25)
    blind_tape = material("Cotton ladder tape", "c9bc9c", .85)
    cork = material("Pressed cork", "8a6240", maps=cork_finish())
    pin_colors = [material(f"Map pin {i + 1}", c, .25) for i, c in enumerate(("a52a22", "e0c34a", "2d5a8c"))]
    manila = material("Manila folder stock", "c8a96a", .80)
    felt = material("Charcoal fur felt", "3f3d39", .92)
    hat_band = material("Black grosgrain", "1a1817", .70)
    milk_glass = material("Milk glass", "f1e8d6", .30)
    milk_bsdf = milk_glass.node_tree.nodes.get("Principled BSDF")
    milk_bsdf.inputs["Emission Color"].default_value = (*color("ffe3b0"), 1)
    milk_bsdf.inputs["Emission Strength"].default_value = 2.4
    daylight_glass = material("Daylit window glass", "e9dcc0", .10)
    day_bsdf = daylight_glass.node_tree.nodes.get("Principled BSDF")
    day_bsdf.inputs["Emission Color"].default_value = (*color("f4e6c8"), 1)
    day_bsdf.inputs["Emission Strength"].default_value = 1.8
    sleeve_stock = [material(f"Sleeve stock {i + 1}", c, .66) for i, c in enumerate(
        ("ad9370", "786348", "c1a580", "433e2a", "967359", "866631", "5b4543", "c3ac86"))]

    group("Menu camera")
    pitch = math.atan((HEIGHT / 2 - 221) / FOCAL)
    yaw = -math.atan((1135 - WIDTH / 2) * math.cos(pitch) / FOCAL)
    camera_pos = Vector((0, 0, 1.245))
    forward = Vector((math.sin(yaw)*math.cos(pitch), math.cos(yaw)*math.cos(pitch), -math.sin(pitch)))
    right = Vector((math.cos(yaw), -math.sin(yaw), 0))
    up = right.cross(forward)
    lens = bpy.data.cameras.new("Office composition")
    lens.lens = FOCAL * 36 / WIDTH
    lens.sensor_fit = "HORIZONTAL"
    lens.clip_start, lens.clip_end = .035, 16
    camera = link(bpy.data.objects.new("Menu camera", lens), "Menu camera")
    camera.location = camera_pos
    camera.rotation_euler = forward.to_track_quat("-Z", "Y").to_euler()
    scene.camera = camera

    room()
    desk_lamp()
    frame()
    records()
    typewriter()
    phone()
    paperwork()
    calendar()
    ashtray()
    lighting()
    export_scene(output)


if __name__ == "__main__":
    reference_path, destination = map(Path, sys.argv[sys.argv.index("--") + 1:])
    build(reference_path, destination)
