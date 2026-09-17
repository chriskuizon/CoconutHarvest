using System.Text.Json.Serialization;

namespace CoconutHarvest.Contracts;

/// <summary>
/// Order matters: it must match the class order used when training the YOLO model
/// (data.yaml -> names). Keep this file and data.yaml in sync.
/// </summary>
public enum DetectionClass
{
    [JsonStringEnumMemberName("bunch_mature")] BunchMature = 0,
    [JsonStringEnumMemberName("bunch_tender")] BunchTender = 1,
    [JsonStringEnumMemberName("bunch_dry")]    BunchDry = 2,
    [JsonStringEnumMemberName("trunk")]        Trunk = 3,
    [JsonStringEnumMemberName("frond")]        Frond = 4,
    [JsonStringEnumMemberName("human")]        Human = 5,
    [JsonStringEnumMemberName("wire")]         Wire = 6,
}

public static class DetectionClassExtensions
{
    // C# 14 extension members
    extension(DetectionClass cls)
    {
        /// <summary>True for classes that must trigger an immediate abort.</summary>
        public bool IsAbortClass => cls is DetectionClass.Human or DetectionClass.Wire;

        /// <summary>True for classes that are valid harvest targets.</summary>
        public bool IsHarvestTarget => cls is DetectionClass.BunchMature;
    }
}
