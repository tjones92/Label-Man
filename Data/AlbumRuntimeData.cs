using System;

[Serializable]
public class AlbumRuntimeData {
	public Album baseAlbum;
	public float eraWeightAtRelease;
	public int retiredTrackResolutionAttempts;
	public int retiredTrackResolutionMisses;

	public AlbumRuntimeData(Album album, int releaseYear) {
		baseAlbum = album;
		eraWeightAtRelease = AlbumModel.GetAlbumEraWeight(releaseYear);
	}
}
