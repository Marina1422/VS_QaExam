using System;
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using VS_QaExam.Models;

namespace VS_QaExam
{
    [TestFixture]
    public class Tests
    {

        private RestClient client;
        private static string lastCreatedMovieId;

        private const string BaseURL = "http://144.91.123.158:5000";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiIwYjQ1NGZhYi1iM2UwLTRmODctYjVmMi01YzY5NjQzYjM1YWIiLCJpYXQiOiIwNC8xOC8yMDI2IDA2OjEwOjE1IiwiVXNlcklkIjoiZjYyMjJkM2ItZGUwZC00NjBiLTYyMmMtMDhkZTc2OTcxYWI5IiwiRW1haWwiOiJNTTIwMjZRQUBlbWFpbC5jb20iLCJVc2VyTmFtZSI6Ik1NXzIwMjZfUUEiLCJleHAiOjE3NzY1MTQyMTUsImlzcyI6Ik1vdmllQ2F0YWxvZ19BcHBfU29mdFVuaSIsImF1ZCI6Ik1vdmllQ2F0YWxvZ19XZWJBUElfU29mdFVuaSJ9.eG6M2nFBdJaZyFrCHExkYCVAxngEA9axuyOx-ygtESc";
        private const string LoginEmail = "MM2026QA@email.com";
        private const string LoginPassword = "mm_1StrongPass";

        [OneTimeSetUp] 
        public void Setup()
        {
            string jwtToken = GetJwtToken(LoginEmail, LoginPassword);

            var options = new RestClientOptions(BaseURL)
            {
                Authenticator = new JwtAuthenticator(jwtToken) //creates new token
            };

            this.client = new RestClient(options);

        }

        private string GetJwtToken(string email, string password) //creating the method, which will recreate setup if authentication fails
        {
            var tempClient = new RestClient(BaseURL);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);


            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("AccessToken property was empty.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status: {response.StatusCode}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateMovie_WithRequiredFields_ShouldReturnsSuccess()
        {
            var movieData = new MovieDTO
            {
                Title = "Test Movie",
                Description = "This is a test movie."
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieData); 
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");

            var responseReceived = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(responseReceived.Movie, Is.Not.Null, "The response should contain a Movie object.");
            Assert.That(responseReceived.Movie.Id, Is.Not.Null.And.Not.Empty, "The returned Movie Id should not be null or empty.");
            Assert.That(responseReceived.Msg, Is.EqualTo("Movie created successfully!"), "Response is as expected.");

            lastCreatedMovieId = responseReceived.Movie.Id;
        }

        [Order(2)]
        [Test]

        public void EditMovie_WithValidData_ShouldReturnsSuccess()
        {
            var editedMovieData = new MovieDTO
            {
                Id = lastCreatedMovieId,
                Title = "Updated Test Movie",
                Description = "This is an updated test movie."
            };
            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", lastCreatedMovieId);
            request.AddJsonBody(editedMovieData); 

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");

            var responseReceived = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            
            Assert.That(responseReceived.Msg, Is.EqualTo("Movie edited successfully!"), "Response is as expected.");
        }


        [Order(3)]
        [Test]  

        public void GetAllMovies_ShouldReturnsSuccess()
        {
            var request = new RestRequest("/api/Catalog/All", Method.Get);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");

            var responseReceived = JsonSerializer.Deserialize<List<MovieDTO>>(response.Content);
            
            Assert.That(responseReceived,Is.Not.Null.And.Not.Empty, "The response should contain a list of movies.");
        }

        [Order(4)]
        [Test]

        public void DeleteExistingMovie_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", lastCreatedMovieId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");

            var responseReceived = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(responseReceived.Msg, Is.EqualTo("Movie deleted successfully!"), "Response is as expected.");
        }

        [Order(5)]
        [Test]

        public void CreateMovie_WithoutRequiredFields_ShouldReturnsBadRequest()
        {
            var movieData = new MovieDTO
            {
                Title = string.Empty,
                Description = string.Empty
            };
            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieData); 

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
        }

        [Order(6)]
        [Test]

        public void EditMovie_WithInvalidId_ShouldReturnsNotFound()
        {
            var editedMovieData = new MovieDTO
            {
                Id = "non-existent-id",
                Title = "Non-Existent Test Movie",
                Description = "This is a non-existent test movie."
            };
            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", "non-existent-id");
            request.AddJsonBody(editedMovieData); 
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");

            var responseReceived = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(responseReceived.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"), "Response is as expected.");
        }

        [Order(7)]
        [Test]

        public void DeleteNonExistingMovie_ShouldReturnNotFound()
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", "non-existent-id");
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");

            var responseReceived = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(responseReceived.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"), "Response is as expected.");
        }



        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}
