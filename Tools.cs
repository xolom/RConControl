﻿using Microsoft.Win32;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.Resources;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Xml;
using RCONManager.Properties;
using System.Xml.Serialization;
using System.Configuration;

namespace RCONManager {
    public class Tools {

        /// <summary>
        /// Get CultureInfo type of two letter ISO
        /// </summary>
        /// <param name="twoLetter">two letter ISO name</param>
        /// <returns>Culture with matching two letter ISO</returns>
        public static CultureInfo GetCultureByTwoLetterISO(String twoLetter) {
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
            var culture = cultures.FirstOrDefault(c =>
                c.TwoLetterISOLanguageName.Equals(twoLetter, StringComparison.InvariantCultureIgnoreCase));
            return culture;
        }

        /// <summary>
        /// Get CultureInfo type of native name 
        /// </summary>
        /// <param name="nativeName">Native name of culture</param>
        /// <returns>Culture with matching native name</returns>
        public static CultureInfo GetCultureByNativeName(String nativeName) {
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
            var culture = cultures.FirstOrDefault(c =>
                c.NativeName.Equals(nativeName, StringComparison.InvariantCultureIgnoreCase));
            return culture;
        }

        /// <summary>
        /// Set Autorun
        /// </summary>
        /// <param name="state">true or false</param>
        public static void SetAutorun(bool state) {
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey(GlobalConstants.AUTORUN_REGKEY, true);

            if (state) rkApp.SetValue(Application.ProductName, Application.ExecutablePath.ToString());
            else rkApp.DeleteValue(Application.ProductName, false);
        }

        /// <summary>
        /// Get all config files in config folder and load its content
        /// </summary>
        /// <returns>List with ConfigFiles</returns>
        public static List<ConfigFile> GetAllConfigFiles() {
            List<ConfigFile> resultList = new List<ConfigFile>();
            string[] fileList = Directory.GetFiles(GlobalConstants.PATH_CONFIGS, "*.cfg");
            foreach (string file in fileList) {
                string[] readFile = File.ReadAllLines(file);

                if (readFile.Length > 0) {
                    string name = null;
                    StringBuilder fileContent = new StringBuilder();

                    foreach (string readLine in readFile) {
                        string line = readLine.Trim();

                        if (line.IndexOf("//") == 0 && line.Contains("Name:")) {
                            int startIdx = line.IndexOf(':') + 1;
                            name = line.Substring(startIdx, line.Length - startIdx);
                        } else if (name == null) {
                            name = file;
                        }
                        if (line.IndexOf("//") != 0 && !String.IsNullOrEmpty(line)) {
                            fileContent.AppendLine(line);
                        }

                    }
                    if (name != null && fileContent.Length > 0) {
                        resultList.Add(new ConfigFile { name = name, content = fileContent.ToString().TrimEnd('\r', '\n') });
                    }
                }
            }
            return resultList;
        }

        /// <summary>
        /// Key Value Pair Class for SettingsSet
        /// </summary>
        /// <typeparam name="T">Type for value</typeparam>
        public class SettingsKeyValue<T> {
            [XmlElement(ElementName = "Key")]
            public string Key { get; set; }
            [XmlElement(ElementName = "Value")]
            public T Value { get; set; }
        }

        /// <summary>
        /// Settings Set class
        /// </summary>
        public class SettingsSet {
            [XmlElement(ElementName = "Strings")]
            public List<SettingsKeyValue<string>> mValuesString = new List<SettingsKeyValue<string>>();
            [XmlElement(ElementName = "Integer")]
            public List<SettingsKeyValue<int>> mValuesInt = new List<SettingsKeyValue<int>>();
            [XmlElement(ElementName = "Bool")]
            public List<SettingsKeyValue<bool>> mValuesBool = new List<SettingsKeyValue<bool>>();
            [XmlElement(ElementName = "ConfigFiles")]
            public List<SettingsKeyValue<ConfigFile>> mValuesConfigFile = new List<SettingsKeyValue<ConfigFile>>();
            [XmlElement(ElementName = "HotKeys")]
            public List<SettingsKeyValue<HotKeyObject>> mValuesHotKeyData = new List<SettingsKeyValue<HotKeyObject>>();

            private Language mLangMan = Language.Instance;

            public void ReadSettings() {
                foreach (SettingsProperty property in Settings.Default.Properties) {
                    if (property.PropertyType == typeof(string)) {
                        mValuesString.Add(new SettingsKeyValue<string> { Key = property.Name, Value = (string)Settings.Default[property.Name] });
                    } else if (property.PropertyType == typeof(int)) {
                        mValuesInt.Add(new SettingsKeyValue<int> { Key = property.Name, Value = (int)Settings.Default[property.Name] });
                    } else if (property.PropertyType == typeof(bool)) {
                        mValuesBool.Add(new SettingsKeyValue<bool> { Key = property.Name, Value = (bool)Settings.Default[property.Name] });
                    } else if (property.PropertyType == typeof(ConfigFile)) {
                        mValuesConfigFile.Add(new SettingsKeyValue<ConfigFile> { Key = property.Name, Value = (ConfigFile)Settings.Default[property.Name] });
                    } else if (property.PropertyType == typeof(HotKeyObject)) {
                        mValuesHotKeyData.Add(new SettingsKeyValue<HotKeyObject> { Key = property.Name, Value = (HotKeyObject)Settings.Default[property.Name] });
                    }
                }
            }

            public void LoadSettings() {
                foreach (SettingsKeyValue<string> value in mValuesString) Settings.Default[value.Key] = value.Value;
                foreach (SettingsKeyValue<int> value in mValuesInt) Settings.Default[value.Key] = value.Value;
                foreach (SettingsKeyValue<bool> value in mValuesBool) Settings.Default[value.Key] = value.Value;
                foreach (SettingsKeyValue<ConfigFile> value in mValuesConfigFile) Settings.Default[value.Key] = value.Value;
                foreach (SettingsKeyValue<HotKeyObject> value in mValuesHotKeyData) Settings.Default[value.Key] = value.Value;
                mLangMan.SwitchLang();
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// Writes settings to XML
        /// </summary>
        /// <param name="path">path to write</param>
        public static void WriteXML(string path) {
            try {
                SettingsSet settings = new SettingsSet();
                settings.ReadSettings();
                XmlSerializer xmls = new XmlSerializer(settings.GetType());
                using (StreamWriter writer = new StreamWriter(path)) {
                    xmls.Serialize(writer, settings);
                }
            } catch (Exception ex) {
                ErrorLogger.Log(ex);
            }
        }

        /// <summary>
        /// Read XML file and loads settings
        /// </summary>
        /// <param name="path">path to read</param>
        public static void ReadXML(string path) {
            try {
                XmlSerializer xmls = new XmlSerializer(typeof(SettingsSet));
                using (StreamReader file = new StreamReader(path)) {
                    SettingsSet settings = (SettingsSet)xmls.Deserialize(file);
                    settings.LoadSettings();
                }
            } catch (Exception ex) {
                ErrorLogger.Log(ex);
            }
        }
    }
}
