﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:2.0.50727.5483
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace XWcfApiService
{
    using System.Runtime.Serialization;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "3.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="Result", Namespace="http://schemas.datacontract.org/2004/07/XWcfApiService")]
    public partial class Result : object, System.Runtime.Serialization.IExtensibleDataObject
    {
        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        private System.Nullable<int> CodeField;
        
        private System.Nullable<int> ErrCodeField;
        
        private string ErrDescField;
        
        private System.Nullable<int> StateField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get
            {
                return this.extensionDataField;
            }
            set
            {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.Nullable<int> Code
        {
            get
            {
                return this.CodeField;
            }
            set
            {
                this.CodeField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.Nullable<int> ErrCode
        {
            get
            {
                return this.ErrCodeField;
            }
            set
            {
                this.ErrCodeField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string ErrDesc
        {
            get
            {
                return this.ErrDescField;
            }
            set
            {
                this.ErrDescField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.Nullable<int> State
        {
            get
            {
                return this.StateField;
            }
            set
            {
                this.StateField = value;
            }
        }
    }
}


[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
[System.ServiceModel.ServiceContractAttribute(Namespace="api.regplat.ru", ConfigurationName="IXWcfApiService")]
public interface IXWcfApiService
{
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/SendCode", ReplyAction="api.regplat.ru/IXWcfApiService/SendCodeResponse")]
    XWcfApiService.Result SendCode(int AppCode, string From, string Phone);
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/RegNewUser", ReplyAction="api.regplat.ru/IXWcfApiService/RegNewUserResponse")]
    XWcfApiService.Result RegNewUser(
                int AppCode, 
                string ExternAccount, 
                string Phone, 
                int Code, 
                string Login, 
                string Password, 
                string LastName, 
                string FirstName, 
                string MiddleName, 
                System.DateTime BirthDate, 
                string AOGuidCityFias, 
                string City, 
                string AOGuidStreetFias, 
                string Street, 
                string AOGuidHouseFias, 
                string House, 
                string Flat, 
                string Email);
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/SendText", ReplyAction="api.regplat.ru/IXWcfApiService/SendTextResponse")]
    XWcfApiService.Result SendText(int AppCode, string From, string To, string Text);
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/SendTextMass", ReplyAction="api.regplat.ru/IXWcfApiService/SendTextMassResponse")]
    XWcfApiService.Result SendTextMass(int AppCode, string From, string Text, string[] To);
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/DeliveryGetRecipientsCount", ReplyAction="api.regplat.ru/IXWcfApiService/DeliveryGetRecipientsCountResponse")]
    XWcfApiService.Result DeliveryGetRecipientsCount(int AppCode, int Sex, int AgeFrom, int AgeTo);
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
public interface IXWcfApiServiceChannel : IXWcfApiService, System.ServiceModel.IClientChannel
{
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
public partial class XWcfApiServiceClient : System.ServiceModel.ClientBase<IXWcfApiService>, IXWcfApiService
{
    
    public XWcfApiServiceClient()
    {
    }
    
    public XWcfApiServiceClient(string endpointConfigurationName) : 
            base(endpointConfigurationName)
    {
    }
    
    public XWcfApiServiceClient(string endpointConfigurationName, string remoteAddress) : 
            base(endpointConfigurationName, remoteAddress)
    {
    }
    
    public XWcfApiServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
            base(endpointConfigurationName, remoteAddress)
    {
    }
    
    public XWcfApiServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
            base(binding, remoteAddress)
    {
    }
    
    public XWcfApiService.Result SendCode(int AppCode, string From, string Phone)
    {
        return base.Channel.SendCode(AppCode, From, Phone);
    }
    
    public XWcfApiService.Result RegNewUser(
                int AppCode, 
                string ExternAccount, 
                string Phone, 
                int Code, 
                string Login, 
                string Password, 
                string LastName, 
                string FirstName, 
                string MiddleName, 
                System.DateTime BirthDate, 
                string AOGuidCityFias, 
                string City, 
                string AOGuidStreetFias, 
                string Street, 
                string AOGuidHouseFias, 
                string House, 
                string Flat, 
                string Email)
    {
        return base.Channel.RegNewUser(AppCode, ExternAccount, Phone, Code, Login, Password, LastName, FirstName, MiddleName, BirthDate, AOGuidCityFias, City, AOGuidStreetFias, Street, AOGuidHouseFias, House, Flat, Email);
    }
    
    public XWcfApiService.Result SendText(int AppCode, string From, string To, string Text)
    {
        return base.Channel.SendText(AppCode, From, To, Text);
    }
    
    public XWcfApiService.Result SendTextMass(int AppCode, string From, string Text, string[] To)
    {
        return base.Channel.SendTextMass(AppCode, From, Text, To);
    }
    
    public XWcfApiService.Result DeliveryGetRecipientsCount(int AppCode, int Sex, int AgeFrom, int AgeTo)
    {
        return base.Channel.DeliveryGetRecipientsCount(AppCode, Sex, AgeFrom, AgeTo);
    }
}