using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using ConsoleApp8.Models;
using RestSharp.Serialization.Json;
using Telegram.Bot.Types.Enums;

namespace ConsoleApp8
{
    internal class Program
    {
        static TelegramBotClient bot = new TelegramBotClient("5413864028:AAFgY8RbjQtiKSBhDcacFmTXc4tOlwFO1JQ");

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            int year = 0;
            Movie randomMovie;
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                if (message.Text != null && message.Text.ToLower() == "/start") //Button "Start" was pressed
                {
                    do
                    {
                        //Рандомим страницу на которой будем искать фильм от 1 до 64 (на 64-й странице рейтинг 7)
                        Random random = new Random();
                        int page = random.Next(1, 64);

                        //Делаем запрос по ip берем 50 результатов из страницы, которую рандомили выше. Максимум из страницы можно достать 50 фильмов
                        
                        var client = new RestClient(
                                $"https://moviesminidatabase.p.rapidapi.com/movie/order/byRating/?page_size=50&page={page}");
                        var request = new RestRequest(Method.GET);
                        request.AddHeader("X-RapidAPI-Key", "00269e84d5msh3a6a436ff9522e8p1f5489jsn93854c5bbb0e");
                        request.AddHeader("X-RapidAPI-Host", "moviesminidatabase.p.rapidapi.com");
                        IRestResponse response = client.Execute(request);

                        //десерилизация полученного результата

                        var deserialize = new JsonDeserializer();
                        var output = deserialize.Deserialize<Dictionary<string, string>>(response);
                        var list = JsonConvert.DeserializeObject<List<Movie>>(output["results"]);

                        //из листа из 50-ти флиьмов рандомим 1
                        randomMovie = list[random.Next(0, list.Count)];

                        //достаем инфу про фильм
                        client = new RestClient(
                                $"https://moviesminidatabase.p.rapidapi.com/movie/id/{randomMovie.imdb_id}/");
                        request = new RestRequest(Method.GET);
                        request.AddHeader("X-RapidAPI-Key", "24a71e0ea3msh55c8e1302d48751p19be5bjsn4deef94609db");
                        request.AddHeader("X-RapidAPI-Host", "moviesminidatabase.p.rapidapi.com");
                        response = client.Execute(request);

                        //десерилизация полученного результата
                        output = deserialize.Deserialize<Dictionary<string, string>>(response);
                        randomMovie = JsonConvert.DeserializeObject<Movie>(output["results"]);

                    } while (randomMovie.year < 2010);//изменить на текущий год минус 10

                    await botClient.SendPhotoAsync(message.Chat, randomMovie.banner,
                        $"Назва фільму: {randomMovie.title}\nЖанр: {randomMovie.GenListToString()} \nРік виходу: {randomMovie.year}\nРейтинг IMDb: {randomMovie.rating}\n",
                        replyMarkup: new InlineKeyboardMarkup(
                            InlineKeyboardButton.WithUrl("Переглянути",
                                $"https://www.google.com/search?q={randomMovie.title}+{randomMovie.year}+%D1%81%D0%BC%D0%BE%D1%82%D1%80%D0%B5%D1%82%D1%8C+%D0%BE%D0%BD%D0%BB%D0%B0%D0%B9%D0%BD&sxsrf=ALiCzsayODZ0C_VwPTF9TBwSuUFkdbSUdg%3A1660508066429&ei=olf5YqziGYbOrgTp2KCACQ&ved=0ahUKEwisu8jLksf5AhUGp4sKHWksCJAQ4dUDCA4&uact=5&oq={randomMovie.title}+{randomMovie.year}+%D1%81%D0%BC%D0%BE%D1%82%D1%80%D0%B5%D1%82%D1%8C+%D0%BE%D0%BD%D0%BB%D0%B0%D0%B9%D0%BD&gs_lcp=Cgdnd3Mtd2l6EAM6BwgjELADECc6BwgAEEcQsAM6BAgjECc6BggjECcQEzoFCAAQgAQ6CggAEMsBEEYQ_wE6BQguEMsBOgUIABDLAToLCC4QxwEQrwEQywE6CwguEMcBENEDEMsBOgYIABAeEBY6BQguEIAESgQIQRgASgQIRhgAUD1Y24oEYOWMBGgCcAF4AIABjAGIAdUQkgEEMjIuMpgBAKABAaABAsgBCsABAQ&sclient=gws-wiz")));

                    //создание кнопок для оценки фильма - вынести в отдельный класс
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] // first row
                        {
                            InlineKeyboardButton.WithCallbackData("Погано😞","-1"),
                            InlineKeyboardButton.WithCallbackData("Нормально🙂","0"),
                            InlineKeyboardButton.WithCallbackData("Вау!😀","1")
                        },
                    });
                    await botClient.SendTextMessageAsync(message.Chat,
                        "Для покращення рекомендацій, будь ласка, оціни фільм🍿", replyMarkup: keyboard);
                }
            }

            if (update.CallbackQuery != null)//если была нажата одна из кнопок с отзывом
            {
                int feedback = int.Parse(update.CallbackQuery.Data);//получаем оценку из сообщения -1, 0, 1
                int n = 3;
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: update.CallbackQuery.Id,
                    text: $"Дякую! Я врахую твій відгук під час наступних рекоменадацій😊",
                    showAlert: true
                );
            }
        }

        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
            return Task.CompletedTask;
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions();
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.ReadLine();
        }
    }
}