using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FableFlow.Application.Common.Heroes;

namespace FableFlow.Api.Tests;

/// <summary>Outils partagés : sérialisation identique au client web (énumérations en texte).</summary>
internal static class ApiTestJson
{
  public static readonly JsonSerializerOptions Options = CreateOptions();

  public static HeroInput Hero(string name = "Léa", int age = 6) =>
      new(name, age, ["Pilou le lapin"], "les volcans", "curieuse", "cheveux bouclés");

  public static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string url, T body) =>
      client.PostAsJsonAsync(url, body, Options);

  public static async Task<T> ReadJsonAsync<T>(this HttpResponseMessage response) =>
      (await response.Content.ReadFromJsonAsync<T>(Options))!;

  private static JsonSerializerOptions CreateOptions()
  {
    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    options.Converters.Add(new JsonStringEnumConverter());
    return options;
  }
}
