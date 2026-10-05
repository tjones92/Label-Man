// Reproducible provisional authoring. Targets are reporting data, never selection quotas.
import fs from 'node:fs';
const path='Data/PolarSongTable.json';
const t=JSON.parse(fs.readFileSync(path,'utf8'));
const rows=[
 ['CountryShuffle',[.45,.40,.50,.55,.40,.20,.45,.30,.75,.65],.65,1900,['Country'],'shuffle','jaunty'],
 ['CountryWaltz',[.35,.55,.45,.45,.45,.25,.25,.45,.80,.65],.65,1900,['Country'],'waltz','yearning'],
 ['WesternSwing',[.40,.45,.70,.75,.35,.30,.40,.55,.65,.60],.55,1930,['Country'],'swing','celebratory'],
 ['NashvilleBallad',[.35,.65,.55,.70,.40,.60,.20,.65,.75,.70],.60,1955,['Country'],'slow','yearning'],
 ['BossaSong',[.25,.60,.65,.60,.45,.35,.20,.75,.65,.60],.50,1958,['Jazz','Latin'],'syncopated','intimate'],
 ['ModernJazzInstrumental',[0,0,.85,.75,0,.35,.40,.85,.60,.60],.40,1945,['Jazz'],'swing','exploratory'],
 ['SurfInstrumental',[0,0,.65,.70,0,.35,.60,.35,.55,.35],.45,1958,['Rock'],'fast','driving'],
 ['QuietHymn',[.30,.45,.35,.35,.40,.15,.15,.35,.95,.75],.55,1900,['Gospel'],'slow','reverent'],
 ['GospelQuartet',[.55,.60,.45,.75,.45,.20,.30,.45,.90,.70],.50,1900,['Gospel'],'measured','communal'],
 ['GospelChoir',[.65,.50,.55,.85,.40,.30,.30,.50,.90,.70],.45,1900,['Gospel'],'dramatic-build','exultant'],
 ['LatinBolero',[.35,.65,.60,.60,.45,.30,.25,.65,.75,.65],.55,1900,['Latin'],'slow','yearning'],
 ['LatinDance',[.45,.45,.65,.75,.35,.30,.40,.55,.65,.45],.55,1900,['Latin','Caribbean'],'fast','celebratory'],
 ['TexMexSong',[.40,.50,.55,.65,.50,.25,.40,.40,.80,.60],.60,1900,['Latin'],'two-beat','narrative'],
 ['ClassicalOrchestral',[0,0,.90,.95,0,.50,.30,.95,.65,.80],.20,1700,['Classical'],'measured','dramatic'],
 ['ClassicalChamber',[0,0,.85,.85,0,.35,.20,.90,.75,.80],.25,1700,['Classical'],'measured','reflective'],
 ['ClassicalSolo',[0,0,.90,.25,0,.25,.20,.90,.75,.80],.25,1700,['Classical'],'measured','reflective']
].map(([name,axes,plasticity,fromYear,families,pace,mood])=>({name,axes,plasticity,fromYear,families,pace,mood,provenance:'Polar repertoire directive Phase 1: provisional unsigned author proposal; review pending.'}));
for(const row of rows){const i=t.archetypes.findIndex(r=>r.name===row.name);if(i<0)t.archetypes.push(row);else t.archetypes[i]=row;}
t.version='polar-repertoire-v1';
fs.writeFileSync(path,JSON.stringify(t,null,2)+'\n');
const assignments={
 'Tin Pan Alley':['LushStandard','SaloonBallad','CharmSong','Swinger','SlowBallad'],
 'Christmas Standard':['QuietHymn','CharmSong','SingAlongChant','SlowBallad'],
 'Jazz Standard':['Swinger','SaloonBallad','CharmSong','ModernJazzInstrumental','LushStandard'],
 'Country Standard':['CountryTwoBeat','CountryShuffle','CountryWaltz','WesternSwing','SlowBallad','VerseDrivenSong','NashvilleBallad'],
 'Blues Standard':['ShuffleTwelveBar','SlowBlues','GrooveRiffVamp'],
 'Gospel Standard':['QuietHymn','GospelQuartet','GospelChoir','SpiritualShout'],
 'Folk Traditional':['VerseDrivenSong','SlowBallad','ProtestMessageSong'],
 'R&B Catalog':['ShuffleTwelveBar','SlowBlues','GrooveRiffVamp','HornDrivenSoulNumber','DeepSoulPleader'],
 'Recent RnR Hit':['StomperRocker','MidTempoRocker','ShuffleTwelveBar','SlowBallad'],
 'Recent R&B Hit':['HornDrivenSoulNumber','DeepSoulPleader','GrooveRiffVamp','ShuffleTwelveBar'],
 'Recent Pop Hit':['BrightPopNumber','MidTempoPopSong','SlowBallad','CharmSong'],
 'Recent Teen Hit':['BrightPopNumber','MidTempoPopSong','DanceNumber','SlowBallad'],
 'Recent DooWop Hit':['SlowBallad','SingAlongChant','MidTempoPopSong'],
 'Recent Country Hit':['CountryTwoBeat','CountryShuffle','NashvilleBallad','CountryWaltz','VerseDrivenSong'],
 'Latin songbook':['LatinBolero','LatinDance'],'TexMex songbook':['TexMexSong','LatinBolero'],
 'Classical works':['ClassicalOrchestral','ClassicalChamber','ClassicalSolo'],
 'Comedy routines':['Novelty','SpokenWord','JauntyMusicHallRomp'],
 'Children songs':['SingAlongChant','Novelty','CharmSong'],
 'Brazilian songbook':['LatinBolero','CharmSong','BossaSong'],
 'Contemporary folk':['VerseDrivenSong','ProtestMessageSong','SlowBallad'],
 'Surf adaptations':['SurfInstrumental','GrooveRiffVamp']
};
const instrumental=new Set(['ModernJazzInstrumental','SurfInstrumental','ClassicalOrchestral','ClassicalChamber','ClassicalSolo']);
const templates=Object.fromEntries(Object.entries(assignments).map(([family,names])=>[family,names.map(archetype=>({archetype,weight:1,
 lyric:instrumental.has(archetype)?'Instrumental':family.includes('Gospel')||archetype==='QuietHymn'?'AdviceExhortation':archetype==='ProtestMessageSong'?'TopicalTestimony':archetype==='SpokenWord'?'McPatterFrame':archetype==='VerseDrivenSong'||archetype==='TexMexSong'?'Narrative':'RomanticAddress',
 vocalPresence:instrumental.has(archetype)?'Instrumental':'Present',density:instrumental.has(archetype)?0:archetype==='VerseDrivenSong'||archetype==='ProtestMessageSong'?0.65:0.40,
 form:archetype.includes('Classical')?'ThroughComposed':archetype==='ShuffleTwelveBar'||archetype==='SlowBlues'?'TwelveBar':archetype==='VerseDrivenSong'||archetype==='QuietHymn'?'Strophic':'Aaba',
 meter:archetype==='CountryWaltz'?'ThreeFour':archetype.includes('Shuffle')?'Shuffle':'FourFour'}))]));
const bands=[['BossaNova','olderSongbook',15,40],['ContemporaryFolk','contemporaryInterpreter',10,35],['Country','mixedContemporary',20,50],['Folk','traditionalRevival',50,90],['Gospel','inheritedRepertoire',50,90],['Jazz','standardsInterpreter',60,90],['Jazz','composerLed',10,50],['RnB','mixedContemporary',5,25],['SurfRock','vocal',0,10],['SurfRock','instrumentalAdaptation',5,25],['EasyListening','standardsLed',50,85]].map(([genre,cohort,min,max])=>({genre,cohort,min,max}));
fs.writeFileSync('Data/PolarRepertoireTable.json',JSON.stringify({version:1,provenance:'Provisional, unsigned authoring under the polar repertoire directive. Historical bands are judgment-based reporting targets, not measured rates.',
 numbers:{demandVariation:0.025,identityVariation:0.02,exactAccess:0.22,secondaryAccess:0.08,familyAccess:0.035,crossSceneAccess:0.015,preferenceDrift:0.005,genrePreferenceWeight:0.08,hookPreference:0.06,familiarityPreference:0.08,suitabilityWindow:0.025,balladBalance:0.05,writingFloor:0.15,exceptionalWriter:0.85,composerLedCutoff:0.65,establishmentAge:15,establishmentDurability:0.5},assignments:templates,bands,
 supply:[
  {family:'Latin songbook',genre:'LatinPop',secondary:'TexMex',count:100,fromYear:1920,toYear:1963,traditional:false},
  {family:'TexMex songbook',genre:'TexMex',secondary:'LatinPop',count:100,fromYear:1900,toYear:1963,traditional:false},
  {family:'Classical works',genre:'Classical',secondary:'Classical',count:120,fromYear:1750,toYear:1959,traditional:false},
  {family:'Comedy routines',genre:'Comedy',secondary:'Comedy',count:60,fromYear:1940,toYear:1963,traditional:false},
  {family:'Children songs',genre:'Childrens',secondary:'Childrens',count:80,fromYear:1900,toYear:1963,traditional:true},
  {family:'Brazilian songbook',genre:'BossaNova',secondary:'LatinPop',count:140,fromYear:1930,toYear:1963,traditional:false},
  {family:'Contemporary folk',genre:'ContemporaryFolk',secondary:'Folk',count:140,fromYear:1958,toYear:1963,traditional:false},
  {family:'Surf adaptations',genre:'SurfRock',secondary:'RockAndRoll',count:60,fromYear:1958,toYear:1963,traditional:false}
 ]},null,2)+'\n');
