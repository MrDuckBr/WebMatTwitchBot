﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebMatBot
{
    public static class Core
    {
        private static ClientWebSocket webSocket { get; set; }

        public static async void Start()
        {
            do
            {
                using (var socket = new ClientWebSocket())
                    try
                    {
                        await socket.ConnectAsync(new Uri("wss://irc-ws.chat.twitch.tv:443"), CancellationToken.None);

                        webSocket = socket;

                        await Send("PASS " + Parameters.OAuth, CancellationToken.None);
                        await Send("NICK " + Parameters.User, CancellationToken.None);
                        await Send("JOIN #" + Parameters.User, CancellationToken.None);

                        

                        await Respond("Estou conectado... Muito bom estar aqui com vcs...");

                        await Receive(CancellationToken.None);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR - {ex.Message}");
                    }
            } while (true);
        }

        public static async Task Send(string data, CancellationToken stoppingToken) =>
        await webSocket.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text, true, stoppingToken);

        public static async Task Receive(CancellationToken stoppingToken)
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);
            while (!stoppingToken.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await webSocket.ReceiveAsync(buffer, stoppingToken);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    ms.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                    {
                        var input = await reader.ReadToEndAsync();
                        Console.WriteLine(input);
                        Analizer(input);
                        Cache.AddToCacheMessage(input);
                    }

                }

            };
        }

        public static async Task Respond(string msg)
        {
            try
            {
                await Send("PRIVMSG #" + Parameters.User + " : MrDestructoid " + msg, CancellationToken.None);
            }
            catch(Exception except)
            {
                Console.WriteLine(except.Message);
            }
        }

        private static async void Analizer(string input)
        {
            //must responde ping pong
            if (input.Contains("PING")) await Send("PONG", CancellationToken.None);

            //check all counters and increase if necessary
            Counters.CheckCounter(input);

            // verifica comandos
            foreach (var cmd in Commands.List)
                if (input.ToLower().Contains(cmd.Key.ToLower())) cmd.Value.Invoke();

        }
    }
}