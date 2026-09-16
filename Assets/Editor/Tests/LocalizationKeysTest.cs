using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.Localization;

/// <summary>
/// Guards against LocalizationKeys constants drifting away from the UI shared string table.
/// </summary>
public class LocalizationKeysTest
{
    [Test]
    public void LocalizationKeys_AllEntryConstants_ExistInUiSharedData()
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationKeys.UiTable);
        Assert.IsNotNull(collection, "UI string table collection is missing. Run PocketMatch > Localization > Create Foundation.");

        var shared = collection.SharedData;
        Assert.IsNotNull(shared, "UI shared data is missing.");

        var keys = typeof(LocalizationKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue())
            .Where(value => value != LocalizationKeys.UiTable)
            .ToArray();

        Assert.IsNotEmpty(keys, "Expected LocalizationKeys entry constants.");

        foreach (var key in keys)
        {
            Assert.IsNotNull(
                shared.GetEntry(key),
                $"Missing UI shared-data entry for LocalizationKeys constant '{key}'.");
        }
    }
}
