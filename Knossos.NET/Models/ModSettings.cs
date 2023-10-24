﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Knossos.NET.Models
{
    /*
        Model for the mod_settings.json file saved at the root of the mod folder to store custom user settings for the mod.
        This works as a "extension" of the mod.json
        All default values must be null for non-change.
    */
    public class ModSettings
    {
        [JsonPropertyName("custom_dependencies")]
        public List<ModDependency>? customDependencies { get; set; } = null;

        [JsonPropertyName("custom_mod_flags")]
        public List<string>? customModFlags { get; set; } = null;

        [JsonPropertyName("custom_build_id")]
        public string? customBuildId { get; set; } = null;

        [JsonPropertyName("custom_build_version")]
        public string? customBuildVersion { get; set; } = null;

        [JsonPropertyName("custom_build_exec")]
        public string? customBuildExec { get; set; } = null;

        [JsonPropertyName("custom_cmdline")]
        public string? customCmdLine { get; set; } = null;

        [JsonPropertyName("is_compressed")]
        public bool isCompressed { get; set; } = false;

        [JsonPropertyName("ignore_global_cmd")]
        public bool ignoreGlobalCmd { get; set; } = false;

        [JsonIgnore]
        private string? filePath = null;

        public ModSettings()
        {
        }

        /// <summary>
        /// Check for non default values in ModSettings
        /// </summary>
        /// <returns>Returns false if user made changes or true if is default config</returns>
        public bool IsDefaultConfig()
        {
            if (customDependencies != null && customDependencies.Any() ||
                customModFlags != null && customModFlags.Any() ||
                customBuildId != null ||
                customBuildVersion != null ||
                customBuildExec != null ||
                customCmdLine != null ||
                ignoreGlobalCmd != false
                )
            {
                return false;
            }
            return true;
        }

        public void Load(string fullPath)
        {
            try
            {
                this.filePath = fullPath + Path.DirectorySeparatorChar + "mod_settings.json";
                if(File.Exists(filePath))
                {
                    using FileStream jsonFile = File.OpenRead(filePath);
                    var tempSettings = JsonSerializer.Deserialize<ModSettings>(jsonFile)!;
                    jsonFile.Close();
                    if (tempSettings != null)
                    {
                        customDependencies = tempSettings.customDependencies;
                        customModFlags = tempSettings.customModFlags;
                        customBuildId = tempSettings.customBuildId;
                        customBuildVersion= tempSettings.customBuildVersion;
                        customCmdLine = tempSettings.customCmdLine;
                        customBuildExec = tempSettings.customBuildExec;
                        isCompressed = tempSettings.isCompressed;
                        ignoreGlobalCmd = tempSettings.ignoreGlobalCmd;
                        Log.Add(Log.LogSeverity.Information, "ModSettings.Load()", "Mod seetings has been loaded from " + filePath);
                    }
                    
                }
                else
                {
                    //Log.Add(Log.LogSeverity.Information, "ModSettings.Load()", "File mod_settings.json " + filePath + " does not exist.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "ModSettings.Load()", ex);
            }
        }

        public void Save()
        {
            try
            {
                if(filePath!= null)
                {
                    var encoderSettings = new TextEncoderSettings();
                    encoderSettings.AllowRange(UnicodeRanges.All);

                    var options = new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.Create(encoderSettings),
                        WriteIndented = true
                    };

                    var json = JsonSerializer.Serialize(this, options);
                    File.WriteAllText(filePath, json, new UTF8Encoding(false));
                    Log.Add(Log.LogSeverity.Information, "ModSettings.Save()", "Mod seetings has been saved to "+filePath);
                }
                else
                {
                    Log.Add(Log.LogSeverity.Error, "ModSettings.Save()", "A mod tried to save mod_settings.json to a null filePath, this happens if you try to Save() without calling Load() first.");
                }
            }
            catch (Exception ex)
            {
                Log.Add(Log.LogSeverity.Error, "ModSettings.Save()", ex);
            }
        }
    }
}
