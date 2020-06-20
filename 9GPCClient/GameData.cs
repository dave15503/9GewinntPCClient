using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace _9GPCClient
{
    class GameData
    {
        [JsonPropertyName("field")]
        public int[][] Field { get; set; }

        [JsonPropertyName("big-field")]
        public int[] BigField { get; set; }

        [JsonPropertyName("active-section")]
        public int ActiveSection { get; set; }

        [JsonPropertyName("active-player")]
        public int ActivePlayer { get; set; }

        [JsonPropertyName("turn")]
        public int Turn { get; set; }

    }
}
