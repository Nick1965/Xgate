﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Oldi.Net.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "14.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<response>\r\n\t<error code=\"{0}\">{1}</error> \r\n</response>\r\n")]
        public string FailResponse {
            get {
                return ((string)(this["FailResponse"]));
            }
            set {
                this["FailResponse"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("11")]
        public int CodeSNA {
            get {
                return ((int)(this["CodeSNA"]));
            }
            set {
                this["CodeSNA"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-8\"?>")]
        public string XmlHeader {
            get {
                return ((string)(this["XmlHeader"]));
            }
            set {
                this["XmlHeader"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("11")]
        public int CodeLongWait {
            get {
                return ((int)(this["CodeLongWait"]));
            }
            set {
                this["CodeLongWait"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("mts")]
        public string providerMTS {
            get {
                return ((string)(this["providerMTS"]));
            }
            set {
                this["providerMTS"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<response>\r\n\t<error code=\"{0}\">{1}</error> \r\n\t<transaction>{2}</transaction>\r\n\t<a" +
            "cceptdate>{3}</acceptdate>\r\n\t<acceptcode>{4}</acceptcode>\r\n\t<account>{5}</accoun" +
            "t>\r\n\t<addinfo>{6}</addinfo>\r\n\t<debt-amount>{7}</debt-amount>\r\n</response>")]
        public string Response {
            get {
                return ((string)(this["Response"]));
            }
            set {
                this["Response"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3")]
        public int CodeSUCS {
            get {
                return ((int)(this["CodeSUCS"]));
            }
            set {
                this["CodeSUCS"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("cyber")]
        public string providerCyber {
            get {
                return ((string)(this["providerCyber"]));
            }
            set {
                this["providerCyber"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("pcc")]
        public string providerPCC {
            get {
                return ((string)(this["providerPCC"]));
            }
            set {
                this["providerPCC"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"windows-1251\"?>")]
        public string XmlHeader1251 {
            get {
                return ((string)(this["XmlHeader1251"]));
            }
            set {
                this["XmlHeader1251"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<response>\r\n\t<error code=\"{0}\">{1}</error> \r\n\t<transaction>{2}</transaction>\r\n\t<a" +
            "cceptdate>{3}</acceptdate>\r\n\t<opname>{4}</opname>\r\n\t<addinfo>{5}</addinfo>\r\n</re" +
            "sponse>")]
        public string rtResponse {
            get {
                return ((string)(this["rtResponse"]));
            }
            set {
                this["rtResponse"] = value;
            }
        }
    }
}
