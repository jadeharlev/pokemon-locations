namespace PokemonLocations.WebServer.Models;

public class UserImagesOptions {
    public string UploadRoot { get; set; } = "/app/uploads";
    public int MaxFilesPerLocation { get; set; } = 20;
    public int MaxBytesPerFile { get; set; } = 10 * 1024 * 1024;          // 10 MB
    public int MaxPixelsPerImage { get; set; } = 50_000_000;              // 50 MP
    public int ResizeLongestEdge { get; set; } = 2000;
}
