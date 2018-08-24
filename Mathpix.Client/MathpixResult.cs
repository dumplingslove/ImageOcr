using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Mathpix.Client
{
    public class MathpixResult
    {
        [JsonProperty("detection_list")]
        public string[] DetectionList { get; set; }
        [JsonProperty("detection_map")]
        public Dictionary<string, double> DetectionMap { get; set; }
        [JsonProperty("error")]
        public string Error { get; set; }
        [JsonProperty("latex")]
        public string Latex { get; set; }
        [JsonProperty("latex_confidence_rate")]
        public double LatexConfidence { get; set; }
        [JsonProperty("position")]
        public MathpixPos Position { get; set; }
        [JsonProperty("latex_list")]
        public string[] LatexList { get; set; }
    }

    public class MathpixPos
    {
        [JsonProperty("width")]
        public int Width { get; set; }
        [JsonProperty("height")]
        public int Height { get; set; }
        [JsonProperty("top_left_x")]
        public int X { get; set; }
        [JsonProperty("top_left_y")]
        public int Y { get; set; }
    }
}
