using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System;
using System.IO;
using NAudio.Wave;

class Program
{
    static readonly string BaseUrl = "https://api.assemblyai.com";
    static readonly string ApiKey = "API-KEY";




    static  void SplitPerMinute(string filePath, string outputDir)
    {
        string inputFile = filePath; // Parçalamak istediğiniz ses dosyasının yolu

        int segmentDuration = 60; // Her bir parçanın süresi (saniye cinsinden)

        Directory.CreateDirectory(outputDir);

        using (var reader = new Mp3FileReader(inputFile))
        {
            int segmentNumber = 1;
            float totalSeconds = (float)reader.TotalTime.TotalSeconds;

            for (float currentSecond = 0; currentSecond < totalSeconds; currentSecond += segmentDuration)
            {
                string outputFileName = $"{Path.GetFileNameWithoutExtension(filePath)}#{segmentNumber}.mp3";
                string outputFilePath = Path.Combine(outputDir, outputFileName);

                using (var writer = File.Create(outputFilePath))
                {
                    float endSecond = Math.Min(currentSecond + segmentDuration, totalSeconds);
                    reader.CurrentTime = TimeSpan.FromSeconds(currentSecond);

                    while (reader.CurrentTime < TimeSpan.FromSeconds(endSecond))
                    {
                        Mp3Frame frame = reader.ReadNextFrame();
                        if (frame != null)
                        {
                            writer.Write(frame.RawData, 0, frame.RawData.Length);
                        }
                    }
                }
                Console.WriteLine($"{outputFileName}: {outputFilePath}");
                segmentNumber++;
            }
        }
    }


    static async Task WriteToFile(string outputDir, string filePath, HttpClient httpClient,int minute)
    {
        var audioUrl = await UploadFileAsync(filePath, httpClient);
        //string audioUrl = "https://assembly.ai/wildfires.mp3";

        var requestData = new
        {
            language_code = "en",
            audio_url = audioUrl,
            speech_model = "universal"
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestData),
            Encoding.UTF8,
            "application/json");

        using var transcriptResponse = await httpClient.PostAsync($"{BaseUrl}/v2/transcript", jsonContent);
        var transcriptResponseBody = await transcriptResponse.Content.ReadAsStringAsync();
        var transcriptData = JsonSerializer.Deserialize<JsonElement>(transcriptResponseBody);

        if (!transcriptData.TryGetProperty("id", out JsonElement idElement))
        {
            throw new Exception("Failed to get transcript ID");
        }

        string transcriptId = idElement.GetString() ?? throw new Exception("Transcript ID is null");

        string pollingEndpoint = $"{BaseUrl}/v2/transcript/{transcriptId}";

        while (true)
        {
            using var pollingResponse = await httpClient.GetAsync(pollingEndpoint);
            var pollingResponseBody = await pollingResponse.Content.ReadAsStringAsync();
            var transcriptionResult = JsonSerializer.Deserialize<JsonElement>(pollingResponseBody);

            if (!transcriptionResult.TryGetProperty("status", out JsonElement statusElement))
            {
                throw new Exception("Failed to get transcription status");
            }

            string status = statusElement.GetString() ?? throw new Exception("Status is null");

            if (status == "completed")
            {
                if (!transcriptionResult.TryGetProperty("text", out JsonElement textElement))
                {
                    throw new Exception("Failed to get transcript text");
                }
                string transcriptText = textElement.GetString() ?? string.Empty;
                Console.WriteLine($"Transcript Text: {transcriptText}");
                string text = "[Dakika-- " + minute.ToString() + " ---Dakika]\n";
                File.AppendAllText(Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(filePath).Split('#')[0]}.txt"), text);
                File.AppendAllText(Path.Combine(outputDir,$"{Path.GetFileNameWithoutExtension(filePath).Split('#')[0]}.txt"), $"{transcriptText}\n");
                break;
            }
            else if (status == "error")
            {
                string errorMessage = transcriptionResult.TryGetProperty("error", out JsonElement errorElement)
                    ? errorElement.GetString() ?? "Unknown error"
                    : "Unknown error";

                throw new Exception($"Transcription failed: {errorMessage}");
            }
            else
            {
                await Task.Delay(3000);
            }
        }

    }


    static async Task<string> UploadFileAsync(string filePath, HttpClient httpClient)
    {
        using (var fileStream = File.OpenRead(filePath))
        using (var fileContent = new StreamContent(fileStream))
        {
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            using (var response = await httpClient.PostAsync("https://api.assemblyai.com/v2/upload", fileContent))
            {
                response.EnsureSuccessStatusCode();
                var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
                // Add null check to fix CS8602 warning
                return jsonDoc?.RootElement.GetProperty("upload_url").GetString() ??
                       throw new InvalidOperationException("Failed to get upload URL from response");
            }
        }
    }

    static async Task Main(string[] args)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("authorization", ApiKey);
        Console.Write("Lütfen ses dosyasının tam yolunu giriniz: (örnek: 'C:\\User\\Desktop\\Audio.mp3'): ");
        string filePath = Console.ReadLine();  //"C:\\Users\\atura\\Downloads\\Tolgahan Tarıoğlu - Unutmak İstiyorum.mp3";
        if (string.IsNullOrWhiteSpace(filePath) && !File.Exists(filePath))
        {
            Console.WriteLine($"Dosya bulunamadı: {filePath}");
            return;
        }

        Console.Write("Lütfen çıktı dizinini giriniz: (örnek: 'C:\\Users\\Temp'): ");
        string outputDir = Console.ReadLine(); //"C:\\Users\\atura\\OneDrive\\Masaüstü\\Whisper";

        if (string.IsNullOrWhiteSpace(outputDir))
        {
            return;
        }
        SplitPerMinute(filePath, outputDir);
        if (File.Exists(Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(filePath).Split('#')[0]}.txt")))
        {
            File.Delete(Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(filePath).Split('#')[0]}.txt"));
        }
        if (Directory.Exists(outputDir))
        {
            string[] files = Directory.GetFiles(outputDir, $"{Path.GetFileNameWithoutExtension(filePath)}*.mp3");
            foreach (var file in files)
            {
                int minute = Array.IndexOf(files, file);
                await WriteToFile(outputDir, Path.Combine(outputDir, Path.GetFileName(file)), httpClient, minute);
            }

            files = Directory.GetFiles(outputDir, $"{Path.GetFileNameWithoutExtension(filePath)}*.mp3");

            foreach (var file in files)
            {
                try
                {
                    File.Delete(Path.Combine(outputDir, Path.GetFileName(file)));
                    Console.WriteLine($"Silindi: {file}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hata oluştu: {file}. Hata: {ex.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine($"Dizin bulunamadı: {outputDir}");
        }
    }



}