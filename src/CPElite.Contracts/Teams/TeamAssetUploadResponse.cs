namespace CPElite.Contracts.Teams;

public sealed record TeamAssetUploadResponse(string Url, string FileName, long SizeBytes, string ContentType);
