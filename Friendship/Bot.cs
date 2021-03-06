﻿using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.InputFiles;

namespace Friendship
{
    class Bot
    {
        public static readonly TelegramBotClient Tgbot = new TelegramBotClient(Settings.Read().TgToken);
        public static List<User> Users;

        public static ReplyKeyboardMarkup Keyboard = new ReplyKeyboardMarkup(new List<List<KeyboardButton>>
        {
            new List<KeyboardButton>() {new KeyboardButton("⚡️ Найти собеседника"), new KeyboardButton("💎 Наш канал")},
            new List<KeyboardButton>() {new KeyboardButton("🔑 Полезные ссылки"), new KeyboardButton("💰 Подписка")},
            new List<KeyboardButton>() {new KeyboardButton("ℹ️ Информация") }, new List<KeyboardButton>(){ new KeyboardButton("🆔 Ваша анкета") }
        });

        public static List<User> WaitList = new List<User>();

        public static void Start()
        {
            using DB db = new DB();
            Users = db.Users.ToList();
            db.Dispose();
            Tgbot.OnMessage += Tgbot_OnMessage;
            Tgbot.OnCallbackQuery += Tgbot_OnCallbackQuery;
            Tgbot.StartReceiving();
        }

        private static async void UsersAdd(string ff)
        {
            try
            {
                if (WaitList.Count == 0) return;
                var waitFf = WaitList.Where(x => x.FindFf == ff).ToList();
                foreach (var user in waitFf)
                {
                    var companion = Users.ToList()
                        .FirstOrDefault(x => x.FindFf == ff && x.Sex == user.findSex && x.Id != user.Id);
                    if (companion == null) continue;
                    WaitList.Remove(user);
                    var username2 = await Tgbot.GetChatMemberAsync(companion.Id, (int) companion.Id);
                    await Tgbot.SendTextMessageAsync(user.Id,
                        $"Собеседник найден:\nИмя пользователя: @{username2.User.Username}\nГород: {companion.City}.\nИнформация: {companion.AboutMe}.");
                    await Tgbot.SendPhotoAsync(companion.Id, new InputOnlineFile(companion.PhotoID));
                    user.state = User.State.main;
                    WaitList.Remove(user);
                }
            }
            catch
            {
                // ignored
            }
        }
        static Random rnd = new Random();
        private static readonly InlineKeyboardMarkup KeyBack = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Назад", "back"));
        private static readonly InlineKeyboardMarkup Keys = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>> {new List<InlineKeyboardButton>{InlineKeyboardButton.WithCallbackData("Фашизм", "find_FH")}, new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("Национализм", "find_NC" )}, new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("Национал-социализм", "find_NS") }, new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("Коммунизм", "find_CM" )}, new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("Либертарианство", "find_LB" )}, new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("Феменизм", "find_FE" )}, new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("Либерализм", "find_LB")}, new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("Назад", "back") } });
        private static async void Tgbot_OnCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            try
            {
                var cb = e.CallbackQuery.Data;
                var user = Users.FirstOrDefault(x => x.Id == e.CallbackQuery.From.Id);
                if (user == null) return;
                if (cb.StartsWith("find"))
                {
                    if (user.state != User.State.find) return;
                    if (e.CallbackQuery.From.Username == null)
                    {
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Для дальнейшего пользования ботом, Вам необходимо установить Имя пользователя (Настройки -> Изменить профиль).");
                        return;
                    }
                    await using DB db = new DB();
                    db.Update(user);
                    string str = cb.Split('_')[1];
                    user.FindFf = str;
                    user.state = User.State.main;
                    await db.SaveChangesAsync();
                    await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Приятного общения.");
                    await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                    UsersAdd(str);
                    return;
                }

                if (cb.StartsWith("bill"))
                {
                    if (Payment.CheckPay(user, cb.Substring(5)))
                    {
                        await using DB db = new DB();
                        db.Update(user);
                        string message = e.CallbackQuery.Message.Text;
                        message = message.Replace("Не оплачено", "Оплачено");
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            message,replyMarkup:KeyBack);
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Успешно оплачено.");
                        user.IsDonate = true;
                        db.SaveChanges();
                    }

                    await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Не оплачено.");
                }

                User companion;
                ChatMember username2;
                List<User> any;
                switch (cb)
                {
                    case "back":
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Назад.");
                        InlineKeyboardMarkup sexKey;
                        switch (user.state)
                        {
                            case User.State.age:
                                user.state = User.State.city;
                                await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                                            "Добро пожаловать, для продолжения необходимо пройти регистрацию. Введите свой город:");
                                break;
                            case User.State.sex:
                                user.state = User.State.age;
                                await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                                            "Отлично, теперь введите свой возраст:", replyMarkup: KeyBack);
                                break;
                            case User.State.aboutmy:
                                await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                                            "Отправьте свое фото.", replyMarkup: KeyBack);
                                user.state = User.State.photo;
                                break;
                            case User.State.photo:
                                sexKey = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                                {
                                    new List<InlineKeyboardButton>
                                    {
                                        InlineKeyboardButton.WithCallbackData("Муж.", "male"),
                                        InlineKeyboardButton.WithCallbackData("Жен.", "female")
                                    },
                                    new List<InlineKeyboardButton>
                                        {InlineKeyboardButton.WithCallbackData("Назад", "back")}
                                });
                                await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                                    "Запомнил, теперь укажите свой пол:", replyMarkup: sexKey);
                                user.state = User.State.sex;
                                break;
                            case User.State.wait:
                                user.state = User.State.selectSex;
                                WaitList.Remove(user);
                                sexKey = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                                {
                                    new List<InlineKeyboardButton>
                                    {
                                        InlineKeyboardButton.WithCallbackData("Муж.", "sex_male"),
                                        InlineKeyboardButton.WithCallbackData("Жен.", "sex_female")
                                    },
                                    new List<InlineKeyboardButton>
                                        {InlineKeyboardButton.WithCallbackData("Назад", "back")}
                                });
                                await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                                            "Выберите пол:", replyMarkup: sexKey);
                                break;
                            default:
                                user.state = User.State.main;
                                await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,e.CallbackQuery.Message.MessageId);
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Вы в главном меню.",
                                    replyMarkup: Keyboard);
                                break;
                        }

                        break;
                    case "male":
                        if (user.state != User.State.sex) return;
                        user.Sex = 1;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Теперь отправь мне свое фото.",
                            replyMarkup: KeyBack);
                        user.state = User.State.photo;
                        break;
                    case "female":
                        if (user.state != User.State.sex) return;
                        user.Sex = 0;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Теперь отправь мне свое фото.",
                            replyMarkup: KeyBack);
                        user.state = User.State.photo;
                        break;
                    case "search":
                        if (user.state != User.State.main) return;
                        user.state = User.State.find;
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id,e.CallbackQuery.Message.MessageId,
                            "Выберите, с кем хотите пообщаться",
                            replyMarkup: Keys);
                        break;
                    case "sex_male":
                    {
                        if (user.state != User.State.selectSex) return;
                        if (!user.IsDonate && user.Sex == 0)
                        {
                            user.state = User.State.wait;
                            await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                                "Для поиска собеседников женского пола необходимо преобрести платную подписку.");
                            SendBill(user);
                            return;
                        }
                        any = Users.ToList().Where(x => x.FindFf == user.FindFf && x.Sex == 1 && x.Id != user.Id).ToList();
                        if (any.Count == 0)
                        {
                            user.findSex = 1;
                            WaitList.Add(user);
                            user.state = User.State.wait;
                            await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                                "Ожидание...", replyMarkup: KeyBack);
                            return;
                        } 
                        companion = any[rnd.Next(0, any.Count)];
                        username2 = await Tgbot.GetChatMemberAsync(companion.Id, (int)companion.Id);
                            await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                                $"Собеседник найден:\nИмя пользователя: @{username2.User.Username}\nГород: {companion.City}.\nИнформация: {companion.AboutMe}.");
                            await Tgbot.SendPhotoAsync(e.CallbackQuery.From.Id, new InputOnlineFile(companion.PhotoID));
                            user.state = User.State.main;
                            if (username2.User.Username == null)
                            {
                                DB db = new DB();
                                db.Update(companion);
                                companion.FindFf = null;
                                await db.SaveChangesAsync();
                                try
                                {
                                    await Tgbot.SendTextMessageAsync(companion.Id, "Похоже, что вы удалили свое Имя пользователя, теперь вас не смоут находить люди. Вам нужно выбрать категорию поиска заново.");
                                }
                                catch
                                {
                                    // ignored
                                }

                                user.state = User.State.selectSex;
                                sexKey = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                                {
                                    new List<InlineKeyboardButton>
                                    {
                                        InlineKeyboardButton.WithCallbackData("Муж.", "sex_male"),
                                        InlineKeyboardButton.WithCallbackData("Жен.", "sex_female")
                                    },
                                    new List<InlineKeyboardButton> {InlineKeyboardButton.WithCallbackData("Назад", "back")}
                                });
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Похоже, что собеседник удалил свое Имя пользователя. Попробуйте еще раз.",
                                    replyMarkup: sexKey);
                            }

                            break;
                    }
                    case "sex_female":
                        if (user.state != User.State.selectSex) return;
                        if (!user.IsDonate && user.Sex==1)
                        {
                            user.state = User.State.wait;
                            await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                                "Для поиска собеседников женского пола необходимо преобрести платную подписку.");
                            SendBill(user);
                            return;
                        }
                        any = Users.ToList().Where(x => x.FindFf == user.FindFf && x.Sex == 1 && x.Id != user.Id).ToList();
                        if (any.Count == 0)
                        {
                            user.findSex = 0;
                            WaitList.Add(user);
                            user.state = User.State.wait;
                            await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                                "Ожидание...", replyMarkup: KeyBack);
                            return;
                        }
                        companion = any[rnd.Next(0, any.Count)];
                        username2 = await Tgbot.GetChatMemberAsync(companion.Id, (int)companion.Id);
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            $"Собеседник найден:\nИмя пользователя: @{username2.User.Username}\nГород: {companion.City}.\nИнформация: {companion.AboutMe}.");
                        await Tgbot.SendPhotoAsync(e.CallbackQuery.From.Id, new InputOnlineFile(companion.PhotoID));
                        user.state = User.State.main;
                        if (username2.User.Username == null)
                        {
                            DB db = new DB();
                            db.Update(companion);
                            companion.FindFf = null;
                            await db.SaveChangesAsync();
                            try
                            {
                                await Tgbot.SendTextMessageAsync(companion.Id, "Похоже, что вы удалили свое Имя пользователя, теперь вас не смоут находить люди. Вам нужно выбрать категорию поиска заново.");
                            }
                            catch
                            {
                                // ignored
                            }

                            user.state = User.State.selectSex;
                            sexKey = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                            {
                                new List<InlineKeyboardButton>
                                {
                                    InlineKeyboardButton.WithCallbackData("Муж.", "sex_male"),
                                    InlineKeyboardButton.WithCallbackData("Жен.", "sex_female")
                                },
                                new List<InlineKeyboardButton> {InlineKeyboardButton.WithCallbackData("Назад", "back")}
                            });
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Похоже, что собеседник удалил свое Имя пользователя. Попробуйте еще раз.",
                                replyMarkup: sexKey);
                        }

                        break;
                    case "change_data":
                        if (user.state != User.State.main) return;
                        user.state = User.State.city;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Введите свой город:",
                            replyMarkup: KeyBack);
                        break;
                    case "change_category":
                        if(user.state!=User.State.main) return;
                        user.state = User.State.find;
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            "Выберите, с кем хотите пообщаться",
                            replyMarkup: Keys);
                        break;
                }
            }
            catch
            {
                var user = Users.FirstOrDefault(x => x.Id == e.CallbackQuery.From.Id);
                if (user != null) user.state = User.State.main;
                // ignored
            }
        }

        private static async void Tgbot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            try
            {
                var message = e.Message;
                if (message.Type != MessageType.Text && message.Type!=MessageType.Photo) return;
                var user = Users.FirstOrDefault(x => x.Id == message.From.Id);
                if (user == null)
                {
                    await using DB db = new DB();
                    user = new User {Id = e.Message.From.Id, state = User.State.main};
                    Users.Add(user);
                    db.Add(user);
                    db.SaveChanges();
                    var key = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Изменить данные профиля", "change_data"));
                        await Tgbot.SendStickerAsync(message.From.Id,
                        new InputOnlineFile("CAACAgIAAxkBAAK_HGAQINBHw7QKWWRV4LsEU4nNBxQ3AAKZAAPZvGoabgceWN53_gIeBA"),replyMarkup: Keyboard);
                    await Tgbot.SendTextMessageAsync(message.Chat.Id,
                        "Добро пожаловать, для продолжения необходимо пройти регистрацию.", replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Создать анкету", "change_data")));
                    return;
                }

                InlineKeyboardMarkup sexKey;
                switch (message.Text)
                {
                    case "🆔 Ваша анкета":
                        if (user.state != User.State.main) return;
                        if (!user.IsRegistered)
                        {
                            await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                "Для начала пройдите регистрацию.", replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Создать анкету", "change_data")));
                            break;
                        }
                        var key = new InlineKeyboardMarkup(new List<InlineKeyboardButton>
                        {
                            InlineKeyboardButton.WithCallbackData("Изменить данные профиля", "change_data"),
                            InlineKeyboardButton.WithCallbackData("Изменить категорию поиска", "change_category")
                        });
                        try
                        {
                            await Tgbot.SendPhotoAsync(e.Message.From.Id, new InputOnlineFile(user.PhotoID),
                                caption: "🌐Ваше фото:");
                        }
                        catch { }

                        string subscribe = (user.IsDonate) ? "Активна" : "Неактивна";
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            $"ℹ️ Ваша анкета\n- - - - - - - - - - - - - - - - - - - - - - - -\n👤 Имя: {e.Message.From.FirstName}\n🔑 Ваш ID: {e.Message.From.Id}\n🏙 Ваш город: {user.City}\n▫️ Ваше описание: {user.AboutMe}\n🛒 Подписка: {subscribe}\n- - - - - - - - - - - - - - - - - - - - - - - -",
                            replyMarkup: key);
                        break;
                    case "/start":
                        if(user.state!=User.State.main) return;
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            "Вы в главном меню.",
                            replyMarkup: Keyboard);
                        break;
                    case "⚡️ Найти собеседника":
                        if (user.state != User.State.main) return;
                        if (!user.IsRegistered)
                        {
                            await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                "Для начала пройдите регистрацию.", replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Создать анкету", "change_data")));
                            break;
                        }
                        if (user.FindFf == null)
                        {
                            await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                "Вам необходимо выбрать категорию поиска. Сделать это можно в анкете.");
                            return;
                        }
                        user.state = User.State.selectSex;
                        sexKey = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                        {
                            new List<InlineKeyboardButton>
                            {
                                InlineKeyboardButton.WithCallbackData("Муж.", "sex_male"),
                                InlineKeyboardButton.WithCallbackData("Жен.", "sex_female")
                            },
                            new List<InlineKeyboardButton> {InlineKeyboardButton.WithCallbackData("Назад", "back")}
                        });
                        await Tgbot.SendTextMessageAsync(message.From.Id,
                            "Выберите пол:", replyMarkup: sexKey);
                        break;
                    case "💎 Наш канал":
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            "‼️ Наш новостной канал:\n✅ @interkriminal");
                        break;
                    case "🔑 Полезные ссылки":
                        const string text = "⚠️Эти приложения помогут Вам сохранить конфиденциальность данных:\n\n1️⃣ Приложение Briar отлично подойдёт тем, кто не доверяет свои конфиденциальные данные популярным социальным сетям.\n<a href=\"https://play.google.com/store/apps/details?id=org.briarproject.briar.android\">СКАЧАТЬ</a>\n\n2️⃣ Tor Browser не нуждается в представлении.\n <a href =\"https://play.google.com/store/apps/details?id=org.torproject.android\">СКАЧАТЬ</a>\n\n3️⃣ Element лучший в своём роде менеджер для приватных бесед.\n<a href=\"https://play.google.com/store/apps/details?id=im.vector.app\">СКАЧАТЬ</a>";
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            text,parseMode:ParseMode.Html, disableWebPagePreview:true);
                        break;
                    case "💰 Подписка":
                        SendBill(user);
                        break;
                    case "ℹ️ Информация":
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            "⭐️ - Бот работает 24/7\n⭐️ -Бот полностью анонимен\n⭐️ -В боте вы можете найти себе соратника / цу ваших интересов\n⭐️ -Бот имеет платную подписку для поиска девушек / парней\n⭐️ -Виды оплат в боте: Qiwi Wallet \n\n❕Важно❕\n- Оплата подписки полностью автоматизирована\n- Что бы оплатить подписку нужно нажать на кнопку \"💰 Подписка\" на основной клавиатуре\n- Подписка оплачивается единоразово\n\n\n👤Техподдержка: @velikorusSS");
                        break;
                    default:
                    {
                        var keyBack = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Назад", "back"));
                        DB db;
                        switch (user.state)
                        {
                            case User.State.city:
                                user.City = message.Text;
                                user.state = User.State.age;
                                await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                    "Отлично, теперь введите свой возраст:", replyMarkup: keyBack);
                                break;
                            case User.State.age:
                                if (!SByte.TryParse(message.Text, out var age))
                                {
                                    await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                        "Пожалуйста, введите свой настоящий возраст!");
                                    return;
                                }

                                user.Age = age;
                                user.state = User.State.sex;
                                sexKey = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                                {
                                    new List<InlineKeyboardButton>
                                    {
                                        InlineKeyboardButton.WithCallbackData("Муж.", "male"),
                                        InlineKeyboardButton.WithCallbackData("Жен.", "female")
                                    },
                                    new List<InlineKeyboardButton>
                                        {InlineKeyboardButton.WithCallbackData("Назад", "back")}
                                });
                                await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                    "Запомнил, теперь укажите свой пол:", replyMarkup: sexKey);
                                break;
                            case User.State.photo:
                                if (message.Type != MessageType.Photo)
                                {
                                    await Tgbot.SendTextMessageAsync(message.Chat.Id, "Отправьте фотографию!");
                                    return;
                                }
                                db = new DB();
                                db.Update(user);
                                user.PhotoID = message.Photo.Last().FileId;
                                await db.SaveChangesAsync();
                                await Tgbot.SendTextMessageAsync(message.From.Id,
                                    "Запомнил, теперь напишите немного о себе. Текст должен быть не короче 10 символов:",replyMarkup:keyBack);
                                    user.state = User.State.aboutmy;
                                break;
                            case User.State.aboutmy:
                            {
                                if (message.Text.Length < 10)
                                {
                                    await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                        "Слишком короткое описание, напиши о себе подробнее.");
                                    return;
                                }

                                if (message.Text.Length > 200)
                                {
                                    await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                        "Ммм... Очень интересно, но слишком длинно. Попробуй уложиться в 200 символов.");
                                    return;
                                }

                                db = new DB();
                                db.Update(user);
                                user.AboutMe = message.Text;
                                user.IsRegistered = true;
                                var find = new InlineKeyboardMarkup(
                                    InlineKeyboardButton.WithCallbackData("Выбрать категорию поиска", "search"));
                                await Tgbot.SendTextMessageAsync(e.Message.From.Id, "Поздравляю с регистрацией.",
                                    replyMarkup: Keyboard);
                                var username = message.From.Username;
                                await Tgbot.SendPhotoAsync(e.Message.From.Id, new InputOnlineFile(user.PhotoID),
                                    caption: "Ваше фото.");
                                await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                    $"Ваша анкета:\nИмя пользователя: @{username}\nГород: {user.City}.\nИнформация: {user.AboutMe}.",
                                    replyMarkup: find);
                                user.state = User.State.main;
                                await db.SaveChangesAsync();
                                break;
                            }
                        }

                        break;
                    }
                }
            }
            catch
            {
                var user = Users.FirstOrDefault(x => x.Id == e.Message.From.Id);
                if (user != null) user.state = User.State.main;
                // ignored
            }
        }

        private static async void SendBill(User user)
        {
            try
            {
                string billId = "";
                int money = 500;
                var payUrl = Payment.AddTransaction(money, user, ref billId);
                if (payUrl == null)
                {
                    await Tgbot.SendTextMessageAsync(user.Id,
                        "Произошла ошибка при создании счета. Попробуйте еще раз.", replyMarkup: KeyBack);
                    return;
                }

                await Tgbot.SendTextMessageAsync(user.Id,
                    $"Оплата подписки на сумму {money} р.\nДата: {DateTime.Now:dd.MMM.yyyy}\nСтатус: Не оплачено.\n\nОплатите счет по ссылке.\n{payUrl}",
                    replyMarkup: new InlineKeyboardMarkup(
                        new List<List<InlineKeyboardButton>>()
                        {
                            new List<InlineKeyboardButton>()
                            {
                                InlineKeyboardButton.WithCallbackData("Проверить оплату", $"bill_{billId}")
                            },
                            new List<InlineKeyboardButton>()
                            {
                                InlineKeyboardButton.WithCallbackData("Назад", "back")
                            }
                        }));
            }
            catch
            {
                if (user != null) user.state = User.State.main;
            }
        }
    }
}
