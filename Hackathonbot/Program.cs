using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Newtonsoft.Json;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types.ReplyMarkups;

namespace Hackathonbot
{
    class Program
    {
        static TelegramBotClient client;

        static async Task Main()
        {
            client = new TelegramBotClient("API_TOKEN");
            using var cts = new CancellationTokenSource();

            client.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                new ReceiverOptions { AllowedUpdates = { } },
                cancellationToken: cts.Token);

            User me = await client.GetMeAsync();

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
            cts.Cancel();
        }


        static readonly long adminId = 856367900;
        static readonly Dictionary<long, Project> projects = new();


        static async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
        {
            UpdateType updateType = update.Type;    // тип обновления

            // если тип обновления тексовое сообщение
            if (updateType == UpdateType.Message)
            {
                string text = update.Message.Text;  // текст сооющения
                long chatId = update.Message.Chat.Id;   // ID чата

                if (text != null && (text.Equals("/start") || text.Equals("/help")))
                {
                    await client.SendTextMessageAsync(chatId, "/question – задать вопрос\n" +
                        "/information – полезная информация и рекомендации\n" +
                        "/cases - кейсы проектов\n" +
                        "/projectdone – сдать готовый проект\n" +
                        "/help – поепзать это сообщение");
                }
                else if (text != null && text.Equals("/question"))
                {
                    InlineKeyboardMarkup mentorsKeyboard = new(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("Ян Швейковский", "mentor 1 @vjwebrus") },
                        new[] { InlineKeyboardButton.WithCallbackData("Сергей Васильев", "mentor 2 @s_vasilyev") },
                        new[] { InlineKeyboardButton.WithCallbackData("Владимир Кожукало", "mentor 3 @Kozhukalovv") },
                        new[] { InlineKeyboardButton.WithCallbackData("Администратор", "mentor admin @arpefly") }
                    });

                    await client.SendTextMessageAsync(chatId, "Выберите кому хотите задать вопрос:",
                        replyMarkup: mentorsKeyboard);
                }   // пользоветель выбирает ментора, которому задать вопрос 
                else if (text != null && text.Equals("/information"))
                {
                    InlineKeyboardMarkup informationKeyboard = new(new[]
                    {
                        new [] { InlineKeyboardButton.WithCallbackData("Расписание хакатона", "schedule") },
                        new [] { InlineKeyboardButton.WithCallbackData("Как распределить роли в команде", "roles") },
                        new [] { InlineKeyboardButton.WithCallbackData("Требования к проектам", "requirements") },
                        new [] { InlineKeyboardButton.WithCallbackData("Процедура выбора победителей", "winners") },
                        new [] { InlineKeyboardButton.WithCallbackData("Памятка подготовки к презентации", "presentation") },
                        new [] { InlineKeyboardButton.WithCallbackData("Термины и обозначения", "terms") },
                        new [] { InlineKeyboardButton.WithCallbackData("Обязанности участника", "memberduties") },
                        new [] { InlineKeyboardButton.WithCallbackData("Обязанности организатора", "organizerduties") }
                    });

                    await client.SendTextMessageAsync(chatId, "Полезная информация и рекомендации:", replyMarkup: informationKeyboard);
                }   // ответы на вопросы в inline режиме
                else if (text != null && text.Equals("/cases"))
                {
                    InlineKeyboardMarkup caseKeyboard = new(new[]
                    {
                        new [] { InlineKeyboardButton.WithCallbackData("Case 1", "case1") },
                        new [] { InlineKeyboardButton.WithCallbackData("Case 2", "case2") },
                        new [] { InlineKeyboardButton.WithCallbackData("Case 3", "case3") },
                        new [] { InlineKeyboardButton.WithCallbackData("Case 4", "case4") },
                        new [] { InlineKeyboardButton.WithCallbackData("Case 5", "case5") },
                    });

                    await client.SendTextMessageAsync(chatId, "Выберите кейс проекта из списка:", replyMarkup: caseKeyboard);
                }
                else if (text != null && text.Equals("/projectdone"))
                {
                    if (!projects.ContainsKey(chatId))
                        projects.Add(chatId, new Project());

                    projects[chatId].AlertMessage = await client.SendTextMessageAsync(chatId, "Если в ходе заполнения формы Вы допустили ошибку её можно иправить после заполнения.");
                    projects[chatId].PrevMessage = await client.SendTextMessageAsync(chatId, "Название команды",
                        replyMarkup: new ForceReplyMarkup() { Selective = true, InputFieldPlaceholder = "Название команды" });
                }   // Проект готов. Начинаем сбор данных по проект. Название команды
                else if (update.Message.ReplyToMessage != null && update.Message.ReplyToMessage.Text == "Название команды")
                {
                    if (projects.ContainsKey(chatId))
                    {
                        projects[chatId].TeamName = text;
                        await client.DeleteMessageAsync(chatId, update.Message.MessageId);
                        await client.DeleteMessageAsync(chatId, projects[chatId].PrevMessage.MessageId);
                    }
                    else
                        await client.SendTextMessageAsync(chatId, "Начните с комнды /projectdone.");

                    projects[chatId].PrevMessage = await client.SendTextMessageAsync(chatId, "Название школы",
                        replyMarkup: new ForceReplyMarkup() { Selective = true, InputFieldPlaceholder = "Название школы" });
                }   // Название команды
                else if (update.Message.ReplyToMessage != null && update.Message.ReplyToMessage.Text == "Название школы")
                {
                    if (projects.ContainsKey(chatId))
                    {
                        projects[chatId].SchoolName = text;
                        await client.DeleteMessageAsync(chatId, update.Message.MessageId);
                        await client.DeleteMessageAsync(chatId, projects[chatId].PrevMessage.MessageId);
                    }
                    else
                        await client.SendTextMessageAsync(chatId, "Начните с комнды /projectdone.");

                    projects[chatId].TeamСompositionMessage = await client.SendTextMessageAsync(chatId, "Состав команды");
                    projects[chatId].PrevMessage = await client.SendTextMessageAsync(chatId, "Командир (фамилия имя email)",
                        replyMarkup: new ForceReplyMarkup() { Selective = true, InputFieldPlaceholder = "Иванов Иван Ivan@gmail.com" });
                }   // Название школы
                else if (update.Message.ReplyToMessage != null && update.Message.ReplyToMessage.Text == "Командир (фамилия имя email)")
                {
                    if (projects.ContainsKey(chatId))
                    {
                        try
                        {
                            projects[chatId].TeamMembers.Add(new TeamMember()
                            {
                                FirstName = text.Split(' ')[1],
                                LastName = text.Split(' ')[0],
                                Email = text.Split(' ')[2],
                                MemberInfo = text
                            });
                        }
                        catch
                        {
                            projects[chatId].TeamMembers.Add(new TeamMember() { MemberInfo = text });
                        }
                        await client.DeleteMessageAsync(chatId, update.Message.MessageId);
                        await client.DeleteMessageAsync(chatId, projects[chatId].PrevMessage.MessageId);
                    }
                    else
                        await client.SendTextMessageAsync(chatId, "Начните с комнды /projectdone.");

                    projects[chatId].PrevMessage = await client.SendTextMessageAsync(chatId, "Второй участник (фамилия имя email)",
                        replyMarkup: new ForceReplyMarkup() { Selective = true, InputFieldPlaceholder = "Иванов Иван Ivan@gmail.com" });
                }   // Соства команды. Командир
                else if (update.Message.ReplyToMessage != null && update.Message.ReplyToMessage.Text == "Второй участник (фамилия имя email)")
                {
                    if (projects.ContainsKey(chatId))
                    {
                        try
                        {
                            projects[chatId].TeamMembers.Add(new TeamMember()
                            {
                                FirstName = text.Split(' ')[1],
                                LastName = text.Split(' ')[0],
                                Email = text.Split(' ')[2],
                                MemberInfo = text
                            });
                        }
                        catch
                        {
                            projects[chatId].TeamMembers.Add(new TeamMember() { MemberInfo = text });
                        }
                        await client.DeleteMessageAsync(chatId, update.Message.MessageId);
                        await client.DeleteMessageAsync(chatId, projects[chatId].PrevMessage.MessageId);
                    }
                    else
                        await client.SendTextMessageAsync(chatId, "Начните с комнды /projectdone.");

                    projects[chatId].PrevMessage = await client.SendTextMessageAsync(chatId, "Третий участник (фамилия имя email)",
                        replyMarkup: new ForceReplyMarkup() { Selective = true, InputFieldPlaceholder = "Третий участник (фамилия имя email)" });
                }   // 2 участник
                else if (update.Message.ReplyToMessage != null && update.Message.ReplyToMessage.Text == "Третий участник (фамилия имя email)")
                {
                    if (projects.ContainsKey(chatId))
                    {
                        try
                        {
                            projects[chatId].TeamMembers.Add(new TeamMember()
                            {
                                FirstName = text.Split(' ')[1],
                                LastName = text.Split(' ')[0],
                                Email = text.Split(' ')[2],
                                MemberInfo = text
                            });
                        }
                        catch
                        {
                            projects[chatId].TeamMembers.Add(new TeamMember() { MemberInfo = text });
                        }
                        await client.DeleteMessageAsync(chatId, update.Message.MessageId);
                        await client.DeleteMessageAsync(chatId, projects[chatId].PrevMessage.MessageId);
                    }
                    else
                        await client.SendTextMessageAsync(chatId, "Начните с комнды /projectdone.");

                    projects[chatId].PrevMessage = await client.SendTextMessageAsync(chatId, "Четвёртый участник (фамилия имя email)",
                        replyMarkup: new ForceReplyMarkup() { Selective = true, InputFieldPlaceholder = "Иванов Иван Ivan@gmail.com" });
                }   // 3 участник
                else if (update.Message.ReplyToMessage != null && update.Message.ReplyToMessage.Text == "Четвёртый участник (фамилия имя email)")
                {
                    if (projects.ContainsKey(chatId))
                    {
                        try
                        {
                            projects[chatId].TeamMembers.Add(new TeamMember()
                            {
                                FirstName = text.Split(' ')[1],
                                LastName = text.Split(' ')[0],
                                Email = text.Split(' ')[2],
                                MemberInfo = text
                            });
                        }
                        catch
                        {
                            projects[chatId].TeamMembers.Add(new TeamMember() { MemberInfo = text });
                        }
                        await client.DeleteMessageAsync(chatId, update.Message.MessageId);
                        await client.DeleteMessageAsync(chatId, projects[chatId].PrevMessage.MessageId);
                        await client.DeleteMessageAsync(chatId, projects[chatId].TeamСompositionMessage.MessageId);
                    }
                    else
                        await client.SendTextMessageAsync(chatId, "Начните с комнды /projectdone.");

                    projects[chatId].PrevMessage = await client.SendTextMessageAsync(chatId, "Номер решаемого кейса",
                        replyMarkup: new ForceReplyMarkup() { Selective = true, InputFieldPlaceholder = "1" });
                }   // 4 участник
                else if (update.Message.ReplyToMessage != null && update.Message.ReplyToMessage.Text == "Номер решаемого кейса")
                {
                    if (projects.ContainsKey(chatId))
                    {
                        projects[chatId].CaseNumber = text;
                        await client.DeleteMessageAsync(chatId, update.Message.MessageId);
                        await client.DeleteMessageAsync(chatId, projects[chatId].PrevMessage.MessageId);
                    }
                    else
                        await client.SendTextMessageAsync(chatId, "Начните с комнды /projectdone.");
                    projects[chatId].PrevMessage = await client.SendTextMessageAsync(chatId, "Название проекта",
                        replyMarkup: new ForceReplyMarkup() { Selective = true, InputFieldPlaceholder = "Название проекта" });
                }   // Номер решаемого кейса
                else if (update.Message.ReplyToMessage != null && update.Message.ReplyToMessage.Text == "Название проекта")
                {
                    if (projects.ContainsKey(chatId))
                    {
                        projects[chatId].ProjetName = text;
                        await client.DeleteMessageAsync(chatId, update.Message.MessageId);
                        await client.DeleteMessageAsync(chatId, projects[chatId].PrevMessage.MessageId);
                    }
                    else
                        await client.SendTextMessageAsync(chatId, "Начните с комнды /projectdone.");

                    projects[chatId].PrevMessage = await client.SendTextMessageAsync(chatId, "Прикрепите презентацию 📎",
                        replyMarkup: new ForceReplyMarkup() { Selective = true, InputFieldPlaceholder = "Прикрепите презентацию" });
                }   // Название проекта
                else if (update.Message.ReplyToMessage != null && update.Message.ReplyToMessage.Text == "Прикрепите презентацию 📎" && update.Message.Document != null)
                {
                    if (projects.ContainsKey(chatId))
                    {
                        projects[chatId].Presentation = await client.GetFileAsync(update.Message.Document.FileId);
                        // Отправление информации администратору
                        await client.SendDocumentAsync(adminId, new InputOnlineFile(projects[chatId].Presentation.FileId),
                            caption: $"<b>Название команды:</b> {projects[chatId].TeamName}\n" +
                            $"<b>Название школы:</b> {projects[chatId].SchoolName}\n" +
                            $"<b>Состав команды:</b>\n" +
                            $"<b>Командир</b>: {projects[chatId].TeamMembers[0].MemberInfo}\n" +
                            $"<b>Участник 2:</b> {projects[chatId].TeamMembers[1].MemberInfo}\n" +
                            $"<b>Участник 3:</b> {projects[chatId].TeamMembers[2].MemberInfo}\n" +
                            $"<b>Участник 4:</b> {projects[chatId].TeamMembers[3].MemberInfo}\n" +
                            $"<b>Название проекта:</b> {projects[chatId].ProjetName}\n" +
                            $"<b>Номер кейса:</b> {projects[chatId].CaseNumber}\n",
                            parseMode: ParseMode.Html);

                        // Сохранение проектов
                        using StreamWriter file = System.IO.File.CreateText("projects.json");
                        JsonSerializer serializer = new() { Formatting = Formatting.Indented };
                        serializer.Serialize(file, projects);

                        await client.SendTextMessageAsync(chatId, "Проект записан. Если вы хотите изменить какое-нибудь поле, заполните форму ещё раз с того же аккаунта Telegram.");
                        await client.DeleteMessageAsync(chatId, projects[chatId].AlertMessage.MessageId);
                        await client.DeleteMessageAsync(chatId, update.Message.MessageId);
                        await client.DeleteMessageAsync(chatId, projects[chatId].PrevMessage.MessageId);
                    }
                    else
                        await client.SendTextMessageAsync(chatId, "Начните с комнды /projectdone.");
                }   // Прикрепление презентации. Скачивание прехентации
                else
                {
                    await client.SendTextMessageAsync(chatId, "/question – задать вопрос\n" +
                        "/information – полезная информация и рекомендации\n" +
                        "/cases - кейсы проектов\n" +
                        "/projectdone – сдать готовый проект\n" +
                        "/help – поепзать это сообщение");
                }
            }
            // если тип обновления CallbackQuery
            else if (updateType == UpdateType.CallbackQuery)
            {
                string query = update.CallbackQuery.Data;   // Текст азапроса
                long chatId = update.CallbackQuery.Message.Chat.Id; // ID чата
                int messageId = update.CallbackQuery.Message.MessageId; // ID сообщения 

                if (query.StartsWith("mentor"))
                {
                    await client.DeleteMessageAsync(chatId, messageId);

                    switch (query.Split(' ')[1])
                    {
                        case "1":
                            await client.SendPhotoAsync(chatId, new InputOnlineFile("AgACAgIAAxkBAAID3WJZJcypyEsPFXbwIQY6ec5152W6AAL7ujEbxjbISk5ulY-YYzMrAQADAgADeQADIwQ"),
                                caption: $"Швейковский Ян Алексеевич, директор VJ STUDIO, сооснователь Чайковского филиала международной школы программирования KiberONE. Вы можете задать интересующий Вас вопрос в личном сообщении: {query.Split(' ')[2]}");
                            break;
                        case "2":
                            await client.SendPhotoAsync(chatId, new InputOnlineFile("AgACAgIAAxkBAAID9mJZKVUITjT640qZd87dEk5rCEACAAO7MRvGNshKRQHtH1DnlQQBAAMCAAN5AAMjBA"),
                                caption: $"Васильев Сергей Вадимович, разработчик IT-решений, бизнес-консультант, тьютор международной школы KiberONE, бизнес-тренер, директор Интернет лаборатории SV-lab, создатель сайта chaiknet.ru. Вы можете задать интересующий Вас вопрос в личном сообщении: {query.Split(' ')[2]}");
                            break;
                        case "3":
                            await client.SendPhotoAsync(chatId, new InputOnlineFile("AgACAgIAAxkBAAID-GJZKezM-VeEofo9TBwLKPc8hofeAAIFuzEbxjbISgJNfE811Mr_AQADAgADeQADIwQ"),
                                caption: $"Вы можете задать интересующий Вас вопрос в личном сообщении: {query.Split(' ')[2]}");
                            break;
                        case "admin":
                            await client.SendTextMessageAsync(chatId, $"Вы можете задать интересующий Вас вопрос в личном сообщении: {query.Split(' ')[2]}");
                            break;
                    }
                }   // Пользователю даётся ссылка на ментора для задания вопроса
                else if (query.Equals("schedule"))
                {
                    await client.EditMessageTextAsync(chatId, messageId, "<b>Расписание хакатона</b>\n\n" +
                        "<b>1.</b> Регистрация и сбор участников (8:30-9:00)\n" +
                        "<b>2.</b> Открытие (9:00-9:30)\n" +
                        "   • Приветствие участников хакатона\n" +
                        "   • Доклад «Секреты проектирования IT-продуктов»\n" +
                        "   • Доклад «Идеальная команда»\n" +
                        "   • Представление проекта «Health Shop»\n" +
                        "<b>3.</b> Командообразование (teambuilding) (9:30-9:50)\n" +
                        "<b>4.</b> Знакомство с менторами, презентация кейсов (9:50-10:10)\n" +
                        "<b>5.</b> Распределение участников команд по мастер-классам (10:10-10:20)\n" +
                        "<b>6.</b> Мастер-классы (10:20-11:00):\n" +
                        "   • Разработка мобильных приложений. Эпоха смартфонов\n" +
                        "   • Проектирование баз данных\n" +
                        "   • Фигма: весь цикл командной работы над прототипом\n" +
                        "   • Бизнес-презентация проектов\n" +
                        "<b>7.</b> Работа над проектами (11:00-12:30)\n" +
                        "<b>8.</b> Обед (12:30-13:00)\n" +
                        "<b>9.</b> Работа над проектами (11:00-12:30)\n" +
                        "<b>10.</b> Подготовка к демофесту (14:00-14:30)\n" +
                        "<b>11.</b> Демофест (14:30-16:00)\n" +
                        "<b>12.</b> Подведение итогов, рефлексия (16:00-16:15)\n" +
                        "<b>13.</b> Награждение победителей, закрытие (16:15-16:30)",
                        parseMode: ParseMode.Html,
                        replyMarkup: new(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("Как распределить роли в команде", "roles") },
                            new [] { InlineKeyboardButton.WithCallbackData("Требования к проектам", "requirements") },
                            new [] { InlineKeyboardButton.WithCallbackData("Процедура выбора победителей", "winners") },
                            new [] { InlineKeyboardButton.WithCallbackData("Памятка подготовки к презентации", "presentation") },
                            new [] { InlineKeyboardButton.WithCallbackData("Термины и обозначения", "terms") },
                            new [] { InlineKeyboardButton.WithCallbackData("Обязанности участника", "memberduties") },
                            new [] { InlineKeyboardButton.WithCallbackData("Обязанности организатора", "organizerduties") }
                        }));
                }   // Расписаниех хакатона
                else if (query.Equals("roles"))
                {
                    await client.EditMessageTextAsync(chatId, messageId, "<b>Возможные роли в команде</b>\n\n" +
                        "<b>Капитан.</b> Избирается участниками команды, представляет интересы команды и принимает организационные решения от имени команды в ходе проведения хакатона\n\n" +
                        "<b>Программист.</b> В команде должен состоять как минимум один участник, уверенно владеющий навыками программирования\n\n" +
                        "<b>Дизайнер.</b> Рекомендуем включить участника, который будет разрабатывать дизайн проекта (иконки, объекты, модели и т. д.)\n\n" +
                        "<b>Менеджер.</b> В качестве совмещения или как отдельная роль, может быть выделен член команды, обладающий лидерскими навыками. Этот человек будет организовывать работу команды, тестировать (принимать результат) и демонстрировать решение жюри, а также презентовать и защищать проект",
                        parseMode: ParseMode.Html,
                        replyMarkup: new(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("Расписание хакатона", "schedule") },
                            new [] { InlineKeyboardButton.WithCallbackData("Требования к проектам", "requirements") },
                            new [] { InlineKeyboardButton.WithCallbackData("Процедура выбора победителей", "winners") },
                            new [] { InlineKeyboardButton.WithCallbackData("Памятка подготовки к презентации", "presentation") },
                            new [] { InlineKeyboardButton.WithCallbackData("Термины и обозначения", "terms") },
                            new [] { InlineKeyboardButton.WithCallbackData("Обязанности участника", "memberduties") },
                            new [] { InlineKeyboardButton.WithCallbackData("Обязанности организатора", "organizerduties") }
                        }));
                }   // Как распределить роли
                else if (query.Equals("requirements"))
                {
                    await client.EditMessageTextAsync(chatId, messageId, "<b>Требования к проектам</b>\n\n" +
                        "На презентацию проектов хакатона и к участию в конкурсной борьбе за призовые места в номинациях будут допущены проекты, соответствующие следующим требованиям:\n" +
                        "   • Целиком и полностью созданным во время проведения хакатона и не являющимся развитием уже существующего проекта\n" +
                        "   • Выполняющим заявленные командой функции\n" +
                        "   • Соответствующим заявленной тематике хакатона",
                        parseMode: ParseMode.Html,
                        replyMarkup: new(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("Расписание хакатона", "schedule") },
                            new [] { InlineKeyboardButton.WithCallbackData("Как распределить роли в команде", "roles") },
                            new [] { InlineKeyboardButton.WithCallbackData("Процедура выбора победителей", "winners") },
                            new [] { InlineKeyboardButton.WithCallbackData("Памятка подготовки к презентации", "presentation") },
                            new [] { InlineKeyboardButton.WithCallbackData("Термины и обозначения", "terms") },
                            new [] { InlineKeyboardButton.WithCallbackData("Обязанности участника", "memberduties") },
                            new [] { InlineKeyboardButton.WithCallbackData("Обязанности организатора", "organizerduties") }
                        }));
                }   // Требования к проектам
                else if (query.Equals("winners"))
                {
                    await client.EditMessageTextAsync(chatId, messageId, "<b>Процедура выбора победителей</b>\n\n" +
                        "   • Выбор победителей хакатона осуществляется жюри на основании оценки проектов участников после их финальной презентации\n" +
                        "   • На основании оценки жюри может быть выбран только один победитель и 3 команды-призера\n" +
                        "   • Жюри производит оценку проектов в соответствии с установленными критериями\n" +
                        "   • По результатам подсчета баллов, которые получила каждая из команд, жюри определяет победителей\n" +
                        "   • В случае спорной ситуации вопрос решается голосованием\n" +
                        "   • Решение жюри является окончательным\n\n" +
                        "Победители награждаются дипломами и памятными призами.\n\n" +
                        "Кроме того, победители имеют возможность получить деловые предложения непосредственно от разработчиков кейсов.",
                        parseMode: ParseMode.Html,
                        replyMarkup: new(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("Расписание хакатона", "schedule") },
                            new [] { InlineKeyboardButton.WithCallbackData("Как распределить роли в команде", "roles") },
                            new [] { InlineKeyboardButton.WithCallbackData("Требования к проектам", "requirements") },
                            new [] { InlineKeyboardButton.WithCallbackData("Памятка подготовки к презентации", "presentation") },
                            new [] { InlineKeyboardButton.WithCallbackData("Термины и обозначения", "terms") },
                            new [] { InlineKeyboardButton.WithCallbackData("Обязанности участника", "memberduties") },
                            new [] { InlineKeyboardButton.WithCallbackData("Обязанности организатора", "organizerduties") }
                        }));
                }   // Процедура выбора победителей
                else if (query.Equals("presentation"))
                {
                    await client.SendDocumentAsync(chatId, new InputOnlineFile("BQACAgIAAxkBAAIEZ2JZqfQ30YX-0OMdYsMEWsbFPAVrAAJtGQACxjbISi8rXxXjlz4sIwQ"),
                        caption: "Пример презентации проекта.");
                }   // Памятка подготовки к презентации
                else if (query.Equals("terms"))
                {
                    await client.EditMessageTextAsync(chatId, messageId,
                        "<b>Термины и обозначения</b>\n\n" +
                        "<b>Участник</b> – физическое лицо, достигшее четырнадцати лет, являющееся резидентом Российской Федерации, действующее от своего имени, и зарегистрировавшееся в соответствии с правилами Положения для участия в хакатоне. Для участия в хакатоне каждый участник должен состоять в команде.\n\n" +
                        "<b>Команда</b> – группа участников, объединившихся для выполнения задания. Каждый участник может входить в состав только одной команды. Количество участников в одной команде ограничено – четыре участника. Жюри оценивает результат команды.\n\n" +
                        "<b>Капитан команды</b> – один из участников команды по выбору команды (избрание оформляется письменно за подписями всех участников команды, с указанием данных, идентифицирующих капитана и участников команды), которому, в случае признания команды победителем хакатона, вручается приз.\n\n" +
                        "<b>Заявка</b> – информация, предоставленная участником хакатона при заполнении и отправке электронной регистрационной формы. Неполные, не соответствующие требованиям настоящего положения заявки организатором не рассматриваются и заявками не признаются.\n\n" +
                        "<b>Задание</b> – требования к содержанию результата и порядку представления, необходимые для выполнения командами проектов в срок, указанный в Положении, и получения возможности выиграть Призы. Задание заключаются в создании результата, определенного организаторами.\n\n" +
                        "<b>Победители хакатона</b> – команды, чьи результаты выполнения задания признаны лучшими в результате оценки жюри на основании критериев, указанных в положении.\n\n" +
                        "<b>Менторы, эксперты, спикеры</b> – представители организатора, осуществляющие консультационную и методическую поддержку команд, IT-специалисты, помогающие командам в реализации проектов и проводящие мастер-классы образовательного характера или выступающие на определенную тему. Ментор участвует в оценке проекта и принятии решения о допуске к финальной презентации проекта.\n\n" +
                        "<b>Жюри</b> – для оценки результатов команд в рамках хакатона создается жюри, состоящее из 5 членов, рекомендованных организаторами хакатона, представителей некоммерческих организаций, экспертов в области разработки приложений, учащихся 10-11 классов технологического профиля. Право решающего голоса предоставляется одному из членов жюри, назначаемому председателем.\n\n" +
                        "<b>Демофест</b> – итоговое событие по защите проектов. Жюри оценивает проекты и выступление команд, по результатам результате определяются победители и призеры.",
                        parseMode: ParseMode.Html,
                        replyMarkup: new(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("Расписание хакатона", "schedule") },
                            new [] { InlineKeyboardButton.WithCallbackData("Как распределить роли в команде", "roles") },
                            new [] { InlineKeyboardButton.WithCallbackData("Требования к проектам", "requirements") },
                            new [] { InlineKeyboardButton.WithCallbackData("Процедура выбора победителей", "winners") },
                            new [] { InlineKeyboardButton.WithCallbackData("Памятка подготовки к презентации", "presentation") },
                            new [] { InlineKeyboardButton.WithCallbackData("Обязанности участника", "memberduties") },
                            new [] { InlineKeyboardButton.WithCallbackData("Обязанности организатора", "organizerduties") }
                        }));
                }   // Термины и обозначения
                else if (query.Equals("memberduties"))
                {
                    await client.EditMessageTextAsync(chatId, messageId, "<b>Обязанности участника</b>\n\n" +
                        "<b>1.</b> Обеспечить сохранность помещения и оборудования, предоставляемых участникам организатором и используемых при проведении хакатона. В случае нанесения материального ущерба возместить сумму ущерба по требованию организатора.\n\n" +
                        "<b>2.</b> Воздерживаться от любых действий, которые могут привести к нанесению ущерба организатору, а также связанные с риском для жизни и здоровья участников.\n\n" +
                        "<b>3.</b> Соблюдать нормы законодательства, в том числе не раскрывать информацию о проектах участников, не передавать контакты третьим лицам во избежание нарушения закона о персональных данных. В случае нарушения настоящего пункта, нарушивший его участник несет ответственность самостоятельно.\n\n" +
                        "<b>4.</b> В проектах не должно содержаться элементов, нарушающих нормы действующего законодательства РФ; изображений персональных данных, объектов исключительных прав, принадлежащих третьим лицам. Также проекты не должны нарушать авторских прав третьих лиц и содержать элементы вирусных, шпионских, следящих программ и иных аналогичных программ.\n\n" +
                        "<b>5.</b> В случае несоблюдения указанных выше обязанностей участник хакатона может быть дисквалифицирован и удален с места проведения мероприятия.",
                        parseMode: ParseMode.Html,
                        replyMarkup: new(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("Расписание хакатона", "schedule") },
                            new [] { InlineKeyboardButton.WithCallbackData("Как распределить роли в команде", "roles") },
                            new [] { InlineKeyboardButton.WithCallbackData("Требования к проектам", "requirements") },
                            new [] { InlineKeyboardButton.WithCallbackData("Процедура выбора победителей", "winners") },
                            new [] { InlineKeyboardButton.WithCallbackData("Памятка подготовки к презентации", "presentation") },
                            new [] { InlineKeyboardButton.WithCallbackData("Термины и обозначения", "terms") },
                            new [] { InlineKeyboardButton.WithCallbackData("Обязанности организатора", "organizerduties") }
                        }));
                }   // Обязанности участника
                else if (query.Equals("organizerduties"))
                {
                    await client.EditMessageTextAsync(chatId, messageId, "<b>Обязанности организатора</b>\n\n" +
                        "<b>1.</b> В ходе Хакатона организатор предоставляет участникам: помещение, столы, стулья, доступ к сети Интернет (при предоставлении участниками MAC-адреса и доступа к настройкам Прокси).\n\n" +
                        "<b>2.</b> Организатор не несет ответственности за сохранность имущества и оборудования участников в месте проведения хакатона.\n\n" +
                        "<b>3.</b> Организатор предоставляет участникам хакатона возможность презентации проектов участников.\n\n" +
                        "<b>4.</b> Организатор создает и предоставляет в общее пользование в рамках хакатона информационную среду в мессенджере Telegram в форме каналов, содержащих следующие материалы: программа хакатона, материалы для кейсов, полезные ссылки, карта локаций, а также чаты участников, регистрацию проектов для демофеста, оповещения.",
                        parseMode: ParseMode.Html,
                        replyMarkup: new(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("Расписание хакатона", "schedule") },
                            new [] { InlineKeyboardButton.WithCallbackData("Как распределить роли в команде", "roles") },
                            new [] { InlineKeyboardButton.WithCallbackData("Требования к проектам", "requirements") },
                            new [] { InlineKeyboardButton.WithCallbackData("Процедура выбора победителей", "winners") },
                            new [] { InlineKeyboardButton.WithCallbackData("Памятка подготовки к презентации", "presentation") },
                            new [] { InlineKeyboardButton.WithCallbackData("Термины и обозначения", "terms") },
                            new [] { InlineKeyboardButton.WithCallbackData("Обязанности участника", "memberduties") }
                        }));
                }   // Обязанности организатора
                else if (query.Equals("case1"))
                {
                    await client.EditMessageTextAsync(chatId, messageId, "<b>Case 1</b>\n\n" +
                        "Компания работает одновременно над 10 проектами для 7 клиентов. В каждом проекте сформирован план задач и работает несколько сотрудников, которые ежедневно ведут работы с фиксацией затраченного времени.\n\n" +
                        "<b>Состав базы данных:</b>\n" +
                        "• Проекты\n" +
                        "• Задачи\n" +
                        "• Сотрудники\n" +
                        "• Учет времени\n\n" +
                        "<b>Ожидаемый функционал:</b>\n" +
                        "• Общая доска проектов с главными данными\n" +
                        "• Доска одного проекта: задачи, общий прогресс, участники и их ежедневные временные затраты\n\n" +
                        "Необходима система контроля общего прогресса по проектам, объемы трудозатрат в целом по проекту и по каждому сотруднику.",
                        parseMode: ParseMode.Html,
                        replyMarkup: new(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("Case 2", "case2") },
                            new [] { InlineKeyboardButton.WithCallbackData("Case 3", "case3") },
                            new [] { InlineKeyboardButton.WithCallbackData("Case 4", "case4") },
                            new [] { InlineKeyboardButton.WithCallbackData("Case 5", "case5") }
                        }));
                }
                else if (query.Equals("case2"))
                {
                    await client.EditMessageTextAsync(chatId, messageId, "<b>Case 2</b>\n\n" +
                        "Открывается новый учебный центр обучения детей программированию. Для прозрачного управления учебной деятельностью требуется создать систему контроля, которая будет учитывать следующие особенности:\n" +
                        "<b>1.</b> Обучение в группах по 10 детей с градацией по возрасту: младшие, средние и старшие.\n" +
                        "<b>2.</b> Обучение длится несколько лет и каждый год набираются три новые группы для всех возрастов. То есть в первый год стартует 3 группы, во второй год плюс еще новые 3 группы и всего их становится 6 и так далее.\n" +
                        "<b>3.</b> Для каждого возраста свой учебный план. У младшей группы длительность полного цикла обучения 8 лет, у средней 6 лет, у старшей 4 года.\n" +
                        "<b>4.</b> Оплата обучения помесячно. Занятия по выходным. В месяц 4 занятия.\n" +
                        "<b>5.</b> Летом каникулы.\n\n" +
                        "<b>Состав базы данных:</b>\n" +
                        "• Ученики\n" +
                        "• Группы/классы\n" +
                        "• Журнал посещаемости\n" +
                        "• Журнал финансов\n" +
                        "• Уроки и учебный план\n\n" +
                        "<b>Ожидаемый функционал от системы контроля:</b>\n" +
                        "<b>1.</b> Возможность составлять учебный план для разных возрастных групп.\n" +
                        "<b>2.</b> Формирование групп детей с выбором учеников и подходящего учебного плана.\n" +
                        "<b>3.</b> Журнал посещаемости.\n" +
                        "<b>4.</b> Финансовый отчет по каждому ученику, с напоминанием когда нужно вносить следующую оплату.",
                        parseMode: ParseMode.Html,
                    replyMarkup: new(new[]
                    {
                            new [] { InlineKeyboardButton.WithCallbackData("Case 1", "case1") },
                            new [] { InlineKeyboardButton.WithCallbackData("Case 3", "case3") },
                            new [] { InlineKeyboardButton.WithCallbackData("Case 4", "case4") },
                            new [] { InlineKeyboardButton.WithCallbackData("Case 5", "case5") }
                    }));
                }
                else if (query.Equals("case3"))
                {
                    await client.EditMessageTextAsync(chatId, messageId, "<b>Case 3</b>\n\n" +
                        "Компания занимается производством газоанализаторов, в ходе работы требуется производить поверку с использованием газовых смесей в баллонах. Баллоны имеют различный объем и могут быть наполнены разным газом.\n\n" +
                        "<b>Состав базы данных:</b>\n" +
                        "• План производства (сроки, количество\n" +
                        "• Список газов (тут одно из полей расход газа на поверку 1 датчика)\n" +
                        "•	База приборов\n" +
                        "•	База баллонов (c/н, объем, газ)\n\n" +
                        "<b>Ожидаемый функционал:</b>\n" +
                        "<b>1.</b> Отчет по плану производства с «подсветкой» позиций, на которые хватает газовых смесей.\n" +
                        "<b>2.</b> Заблаговременное уведомление ответственных о необходимости закупки газовых смесей.\n" +
                        "<b>3.</b> Автоматическое списание использованных баллонов.\n" +
                        "<b>4.</b> Автоматическое формирования списка необходимых для производства партии баллонов.",
                        parseMode: ParseMode.Html,
                    replyMarkup: new(new[]
                    {
                            new [] { InlineKeyboardButton.WithCallbackData("Case 1", "case1") },
                            new [] { InlineKeyboardButton.WithCallbackData("Case 2", "case2") },
                            new [] { InlineKeyboardButton.WithCallbackData("Case 4", "case4") },
                            new [] { InlineKeyboardButton.WithCallbackData("Case 5", "case5") }
                    }));
                }
                else if (query.Equals("case4"))
                {
                    await client.EditMessageTextAsync(chatId, messageId, "<b>Case 4</b>\n\n" +
                        "Кондитерская планирует печь торты. Для производства тортов используют различные ингредиенты.\n\n" +
                        "<b>Состав базы данных:</b>\n" +
                        "• План производства (сроки, количество)\n" +
                        "• Список ингредиентов\n" +
                        "• База тортов\n" +
                        "• База ингредиентов\n\n" +
                        "<b>Требуемый функционал:</b>\n" +
                        "• Отчет по плану производства с «подсветкой» позиций, на которые хватает ингредиентов\n" +
                        "• Заблаговременное уведомление ответственных о необходимости закупки ингредиентов с учетом срока годности\n" +
                        "• Автоматическое формирования списка ингредиентов в соответствии с рецептом",
                        parseMode: ParseMode.Html,
                        replyMarkup: new(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("Case 1", "case1") },
                            new [] { InlineKeyboardButton.WithCallbackData("Case 2", "case2") },
                            new [] { InlineKeyboardButton.WithCallbackData("Case 3", "case3") },
                            new [] { InlineKeyboardButton.WithCallbackData("Case 5", "case5") }
                        }));
                }
                else if (query.Equals("case5"))
                {
                    await client.EditMessageTextAsync(chatId, messageId, "<b>Case 5</b>\n\n" +
                        "Необходимо автоматизировать работу отдела кадров на предприятии путем внедрения программно-аппаратного комплекса.\n\n" +
                        "<b>Требуемый функционал:</b>\n" +
                        "<b>1.</b> Справочник сотрудников:\n" +
                        "   • ФИО\n" +
                        "   • Фото\n" +
                        "   • Отдел\n" +
                        "   • Должность\n" +
                        "   • Рабочее место\n" +
                        "   • Телефон мобильный\n" +
                        "   • Телефон стационарный\n" +
                        "   • Электронная почта\n" +
                        "<b>2.</b> Автоматизировать учет рабочего времени, т.е. фиксировать приход на работу/уход с работы сотрудников (любой вариант: сканирование личного кода, приложить карту, отпечаток пальца, Face ID и т.д.)\n" +
                        "<b>3.</b> Формирование отчета о присутствии сотрудников на рабочем месте.\n" +
                        "<b>4.</b> База отпусков, административных дней и часов, больничных, командировок.Формирование отчета по ней\n" +
                        "<b>5.</b> Возможно сделать интеграцию отчетов о присутствии сотрудника на работе в справочник.",
                        parseMode: ParseMode.Html,
                    replyMarkup: new(new[]
                    {
                            new [] { InlineKeyboardButton.WithCallbackData("Case 1", "case1") },
                            new [] { InlineKeyboardButton.WithCallbackData("Case 2", "case2") },
                            new [] { InlineKeyboardButton.WithCallbackData("Case 3", "case3") },
                            new [] { InlineKeyboardButton.WithCallbackData("Case 4", "case4") }
                    }));
                }

                await client.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
            }
        }

        static async Task<Task> HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(exception.Message);

            await client.SendTextMessageAsync(adminId, $"ERROR: {exception.Message}");

            return new Task(new Action(() => {  }));
        }
    }
}