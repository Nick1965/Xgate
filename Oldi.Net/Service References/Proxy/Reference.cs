﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.36213
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Oldi.Net.Proxy {
    using System.Runtime.Serialization;
    using System;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="Response", Namespace="http://schemas.datacontract.org/2004/07/XSMPP")]
    [System.SerializableAttribute()]
    public partial class Response : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [System.NonSerializedAttribute()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private int errCodeField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private string errDescField;
        
        [global::System.ComponentModel.BrowsableAttribute(false)]
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
            get {
                return this.extensionDataField;
            }
            set {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public int errCode {
            get {
                return this.errCodeField;
            }
            set {
                if ((this.errCodeField.Equals(value) != true)) {
                    this.errCodeField = value;
                    this.RaisePropertyChanged("errCode");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string errDesc {
            get {
                return this.errDescField;
            }
            set {
                if ((object.ReferenceEquals(this.errDescField, value) != true)) {
                    this.errDescField = value;
                    this.RaisePropertyChanged("errDesc");
                }
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="Proxy.IXSMPP")]
    public interface IXSMPP {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IXSMPP/Send", ReplyAction="http://tempuri.org/IXSMPP/SendResponse")]
        Oldi.Net.Proxy.Response Send(string from, string phone, string text);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IXSMPPChannel : Oldi.Net.Proxy.IXSMPP, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class XSMPPClient : System.ServiceModel.ClientBase<Oldi.Net.Proxy.IXSMPP>, Oldi.Net.Proxy.IXSMPP {
        
        public XSMPPClient() {
        }
        
        public XSMPPClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public XSMPPClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public XSMPPClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public XSMPPClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public Oldi.Net.Proxy.Response Send(string from, string phone, string text) {
            return base.Channel.Send(from, phone, text);
        }
    }
}
