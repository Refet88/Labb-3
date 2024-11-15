using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Labb_3
{
    internal class ImportOpenTriviaDatabase
    {
        public class Category
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class CategoriesResponse
        {
            [JsonProperty("trivia_categories")]
            public List<Category> Categories { get; set; }
        }

        public class TriviaQuestion
        {
            [JsonProperty("category")]
            public string Category { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("difficulty")]
            public string Difficulty { get; set; }

            [JsonProperty("question")]
            public string Question { get; set; }

            [JsonProperty("correct_answer")]
            public string CorrectAnswer { get; set; }

            [JsonProperty("incorrect_answers")]
            public List<string> IncorrectAnswers { get; set; }
        }

        public class QuestionsResponse
        {
            [JsonProperty("response_code")]
            public int ResponseCode { get; set; }

            [JsonProperty("results")]
            public List<TriviaQuestion> Questions { get; set; }
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync("https://opentdb.com/api_category.php");
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var categoriesResponse = JsonConvert.DeserializeObject<CategoriesResponse>(json);
                    return categoriesResponse.Categories;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching categories: {ex.Message}");
                return new List<Category>(); // Returnera en tom lista vid fel
            }
        }

        public async Task<List<TriviaQuestion>> GetQuestionsAsync(string category, string difficulty, int amount)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var url = $"https://opentdb.com/api.php?amount={amount}&category={category}&difficulty={difficulty}&type=multiple";
                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"API Response: {json}"); // Logga svaret

                    // Lägg till denna kod för att kontrollera deserialiseringen
                    var questionsResponse = JsonConvert.DeserializeObject<QuestionsResponse>(json);
                    if (questionsResponse != null && questionsResponse.Questions != null)
                    {
                        foreach (var question in questionsResponse.Questions)
                        {
                            Debug.WriteLine($"Question: {question.Question}, Correct Answer: {question.CorrectAnswer}, Incorrect Answers: {string.Join(", ", question.IncorrectAnswers)}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Deserialization returned null or no questions.");
                    }

                    return questionsResponse?.Questions ?? new List<TriviaQuestion>(); // Returnera frågor eller en tom lista
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching questions: {ex.Message}");
                return new List<TriviaQuestion>(); // Returnera en tom lista vid fel
            }
        }
    }
}
