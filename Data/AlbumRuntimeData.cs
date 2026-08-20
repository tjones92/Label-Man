using System;

[Serializable]
public class AlbumRuntimeData {
	public Album baseAlbum;
	public float eraWeightAtRelease;
	public int retiredTrackResolutionAttempts;
	public int retiredTrackResolutionMisses;

	/// <summary>Parameterless ctor for the full-world save's deserializer (System.Text.Json).</summary>
	public AlbumRuntimeData() { }

	public AlbumRuntimeData(Album album, int releaseYear) {
		baseAlbum = album;
		eraWeightAtRelease = AlbumModel.GetAlbumEraWeight(releaseYear);
	}
}
