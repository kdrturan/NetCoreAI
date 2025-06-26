using NetCoreAI.Project03_RapidAPI.ViewModels;
using Newtonsoft.Json;
using System.Net.Http.Headers;


var client = new HttpClient();
List<ApiSeriesViewModel> series = new List<ApiSeriesViewModel>();
var request = new HttpRequestMessage
{
    Method = HttpMethod.Get,
    RequestUri = new Uri("https://imdb-top-100-movies.p.rapidapi.com/"),
    Headers =
    {
        { "x-rapidapi-key", "87a08152fdmsh5dd6a5f2fb7dd58p1d87d7jsn134f2bf68104" },
        { "x-rapidapi-host", "imdb-top-100-movies.p.rapidapi.com" },
    },
};
using (var response = await client.SendAsync(request))
{
    response.EnsureSuccessStatusCode();
    var body = await response.Content.ReadAsStringAsync();
    series = JsonConvert.DeserializeObject<List<ApiSeriesViewModel>>(body);
    foreach (var s in series)
    {
        Console.WriteLine(s.rank + "-) Tittle: " + s.title + " -Year: " + s.year +  " -Rating: " + s.rating);

    }
}
Console.ReadLine();