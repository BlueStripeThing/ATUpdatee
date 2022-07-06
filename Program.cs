using System;
using System.Threading;
using System.Threading.Tasks;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Extensions.Polling;

using Newtonsoft.Json;
using System.Data.SqlClient;

namespace ATUpdaterBot
{
    struct ReceivedMessage
    {
        public long chatId;
        public string? userName;
        public string message;
        public DateTime timestamp;
    }

    class Program
    {

        private static TelegramBotClient bot = new TelegramBotClient("");
        private static String connectionString = "";


        static string messageLogFile = "ATUpdatesMessageLogs.json";
        static List<ReceivedMessage> messagesList = new List<ReceivedMessage>();


        static void Main(string[] args)
        {
            //Чтение файла логов
            try
            {
                var messagesListJson = System.IO.File.ReadAllText(messageLogFile);
                messagesList = JsonConvert.DeserializeObject<List<ReceivedMessage>>(messagesListJson) ?? messagesList;
            }
            catch (Exception exc)
            {
                Console.WriteLine("Error: reading or deserializing " + exc);
            }

            //Запуск бота
            ReceiverOptions receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new UpdateType[]
                {
                    UpdateType.Message
                }
            };
            bot.StartReceiving(UpdateHandler, ErrorHandler, receiverOptions);
            Console.ReadLine();
        }

        private static async Task ErrorHandler(ITelegramBotClient bot, Exception exception, CancellationToken cancelToken)
        {
            Console.WriteLine(JsonConvert.SerializeObject(exception));

        }

        //Обработка сообщений
        private static async Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken cancelToken)
        {
            if (update.Type == UpdateType.Message)
            {
                if (update.Message.Type == MessageType.Text)
                {
                    ReceivedMessage _newMessage = new ReceivedMessage
                    {
                        chatId = update.Message.Chat.Id,
                        userName = update.Message.Chat.Username,
                        timestamp = update.Message.Date.ToLocalTime(),
                        message = update.Message.Text
                    };

                    Console.WriteLine(update.Message.Text);

                    //Обработка команд
                    if (_newMessage.message[0] == '/') 
                    {
                        if (_newMessage.message == "/start") Start(_newMessage);
                        else
                        {
                            switch (_newMessage.message.Substring(1, _newMessage.message.IndexOf(" ") - 1))
                            {
                                case "addsub":
                                    Console.WriteLine("Добавление подписки");
                                    AddSubscription(GetBookInfo(_newMessage.message.Substring(_newMessage.message.IndexOf(" ")), _newMessage.chatId), _newMessage.chatId);
                                    break;
                                case "bookinfo":
                                    GetBookInfo(_newMessage.message.Substring(_newMessage.message.IndexOf(" ")), _newMessage.chatId);
                                    break;
                                case "showsubs":
                                    GetBookInfo(_newMessage.message.Substring(_newMessage.message.IndexOf(" ")), _newMessage.chatId);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    //логирование
                    messagesList.Add(_newMessage);
                    var messagesListJson = JsonConvert.SerializeObject(messagesList);
                    System.IO.File.WriteAllText(messageLogFile, messagesListJson);

                    Console.WriteLine(_newMessage.userName + " " + _newMessage.message);

                }
            }
        }

        //Посмотреть какая сейчас глава книги   -> добавить книгу в бд 
        private async static void AddSubscription(Book book, long Person_id) // Переделать в метод информации о книге, и отдельно метод подписки
        {
            Subscription sub = new Subscription(book,Person_id,DateTime.Now.ToLocalTime());
            sub.ToDB(connectionString);
        }


        private async static void Start(ReceivedMessage message) 
        {
            Person person = new Person();
            person.id = message.chatId;
            person.start_date = DateTime.Now.ToLocalTime();
            person.username = message.userName;
            person.ToDB(connectionString);
            Console.WriteLine("Пользователь добавлен в базу");
        }

        private async static void ShowSubscriptions(long Person_id) // Переделать в метод информации о книге, и отдельно метод подписки
        {

        }

        private static Book GetBookInfo(string link, long chatId) // перетащить в книгу
        {
            Book book = new Book();
            try
            {
                string lastChapterName = "";            
                using (HttpClientHandler web = new HttpClientHandler())
                {
                    using (var client = new HttpClient(web))
                    {
                        using (HttpResponseMessage site = client.GetAsync(link).Result)
                        {
                            if (site.IsSuccessStatusCode)
                            {
                                var html = site.Content.ReadAsStringAsync().Result;
                                if (!string.IsNullOrEmpty(html))
                                {
                                    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                                    doc.LoadHtml(html);

                                    //Парсинг глав
                                    var chapters = doc.DocumentNode.SelectNodes("/html/body/div/div/section/div/div/div/div/div/div/div/div/div/div/ul/li");
                                    if (chapters != null && chapters.Count > 0)
                                    {
                                        book.chapters = chapters.Count;
                                        foreach (var chapter in chapters)
                                        {
                                            var ashki = chapter.ChildNodes.FindFirst("a");
                                            if (ashki != null)
                                            {
                                                lastChapterName = ashki.InnerText;
                                            }
                                            else
                                            {
                                                lastChapterName = chapter.SelectNodes("span").First().InnerText;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Глав нет");
                                    }

                                    //Парсинг атрибутов книги
                                    book.title = doc.DocumentNode.SelectSingleNode("html/body/div/div/section/div/div/div[1]/div/div/div/div/h1").InnerText.Trim();
                                    book.author = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div/section/div/div/div[1]/div[1]/div/div/div[2]/div[1]/span/a").InnerText.Trim();

                                    if (doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div/section/div/div/div[1]/div[1]/div/div/div[2]/div[2]/div[2]/span[3]").HasAttributes)
                                        book.mod_date = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div/section/div/div/div[1]/div[1]/div/div/div[2]/div[2]/div[2]/span[3]").GetAttributeValue("data-time", "Неизвестно").Trim();
                                    else book.mod_date = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div/section/div/div/div[1]/div[1]/div/div/div[2]/div[2]/div[2]/span[3]/span").GetAttributeValue("data-time", "Неизвестно").Trim();
                                   // bot.SendTextMessageAsync(chatId, $"Книга: {book.title}.\nПоследняя глава: {lastChapterName}");
                                }
                            }
                        }
                    }
                }
                book.id = link.Substring(link.IndexOf("work/") + 5);                
            }
            catch (Exception exc) { Console.WriteLine(exc.Message); }
            return book;
        }
    }
}
