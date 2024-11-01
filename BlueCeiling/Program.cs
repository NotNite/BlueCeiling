﻿using System.Globalization;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using FishyFlip;
using FishyFlip.Events;
using FishyFlip.Models;

namespace BlueCeiling;

public partial class Program {
    private const string LightUrl = "https://soulja-boy-told.me/light";
    private static ATJetStream JetStream = new ATJetStreamBuilder().Build();
    private static HttpClient HttpClient = new();

    public static async Task Main() {
        JetStream.OnRecordReceived += OnRecordReceived;
        JetStream.OnConnectionUpdated += OnConnectionUpdated;
        await JetStream.ConnectAsync();
        await Task.Delay(-1);
    }

    private static void OnConnectionUpdated(object? _, SubscriptionConnectionStatusEventArgs args) {
        if (args.State is WebSocketState.Closed or WebSocketState.Aborted or WebSocketState.None) {
            Console.WriteLine("got ratio'd, reconnecting");
            Task.Run(() => JetStream.ConnectAsync());
        }
    }

    private static void OnRecordReceived(object? _, JetStreamATWebSocketRecordEventArgs args) {
        try {
            if (args.Record?.Commit?.Record is not Post post) return;
            if (post.Text is null) return;
            Process(post.Text, post.CreatedAt);
        } catch {
            // ignored
        }
    }

    private static void Process(string text, DateTime? createdAt = null) {
        var match = HexColorRegex().Match(text);
        if (match.Success) {
            var (r, g, b) = HexToRgb(match.Value);
            var msAgo = createdAt is not null
                            ? (DateTime.UtcNow - createdAt.Value).TotalMilliseconds
                            : 0;
            Console.WriteLine($"Firing {r:X2}{g:X2}{b:X2} (message age was {msAgo}ms)");
            Task.Run(() => Fire(r, g, b));
        }
    }

    private static async Task Fire(byte r, byte g, byte b) =>
        await HttpClient.GetAsync($"{LightUrl}?r={r}&g={g}&b={b}");

    private static (byte r, byte g, byte b) HexToRgb(string hex) {
        hex = hex.TrimStart('#');
        var r = byte.Parse(hex[..2], NumberStyles.HexNumber);
        var g = byte.Parse(hex[2..4], NumberStyles.HexNumber);
        var b = byte.Parse(hex[4..6], NumberStyles.HexNumber);
        return (r, g, b);
    }

    [GeneratedRegex(@"#([A-Fa-f0-9]{6})(?:\s|$)")]
    private static partial Regex HexColorRegex();
}
