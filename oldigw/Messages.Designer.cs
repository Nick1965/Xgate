﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.36213
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Oldi.Net {
    using System;
    
    
    /// <summary>
    ///   Класс ресурса со строгой типизацией для поиска локализованных строк и т.д.
    /// </summary>
    // Этот класс создан автоматически классом StronglyTypedResourceBuilder
    // с помощью такого средства, как ResGen или Visual Studio.
    // Чтобы добавить или удалить член, измените файл .ResX и снова запустите ResGen
    // с параметром /str или перестройте свой проект VS.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Messages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Messages() {
        }
        
        /// <summary>
        ///   Возвращает кэшированный экземпляр ResourceManager, использованный этим классом.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Oldi.Net.Messages", typeof(Messages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Перезаписывает свойство CurrentUICulture текущего потока для всех
        ///   обращений к ресурсу с помощью этого класса ресурса со строгой типизацией.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на tid={0} [STATUS] {1}.
        /// </summary>
        internal static string ErrDesc {
            get {
                return ResourceManager.GetString("ErrDesc", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на [Payment] Ошибка шлюза {0}.
        /// </summary>
        internal static string InternalPaymentError {
            get {
                return ResourceManager.GetString("InternalPaymentError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на tid={0} [STATUS] {1}\r\n{2}.
        /// </summary>
        internal static string LogError {
            get {
                return ResourceManager.GetString("LogError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Платёж отменён оператором.
        /// </summary>
        internal static string ManualUndo {
            get {
                return ResourceManager.GetString("ManualUndo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на [Processing] Ошибка разбора {0} {1}.
        /// </summary>
        internal static string ParseError {
            get {
                return ResourceManager.GetString("ParseError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Tid={0} [STATUS] Платёж не найден..
        /// </summary>
        internal static string PayNotFound {
            get {
                return ResourceManager.GetString("PayNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на {0} [STAUS] - {1}.
        /// </summary>
        internal static string StatusRequest {
            get {
                return ResourceManager.GetString("StatusRequest", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Ожидается платёж на сумму.
        /// </summary>
        internal static string SumWait {
            get {
                return ResourceManager.GetString("SumWait", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на {0} [UNDO] Запрос отмены платежа в ЕСПП.
        /// </summary>
        internal static string UndoRequest {
            get {
                return ResourceManager.GetString("UndoRequest", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Неизвестный провайдер {0}.
        /// </summary>
        internal static string UnknownProvider {
            get {
                return ResourceManager.GetString("UnknownProvider", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Неизвестный запрос {0}.
        /// </summary>
        internal static string UnknownRequest {
            get {
                return ResourceManager.GetString("UnknownRequest", resourceCulture);
            }
        }
    }
}
