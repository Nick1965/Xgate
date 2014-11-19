﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:2.0.50727.5485
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
        
        private System.Nullable<decimal> BalanceField;
        
        private System.Nullable<int> CardField;
        
        private System.Nullable<decimal> CommissionField;
        
        private System.Nullable<int> CommissionTypeField;
        
        private System.Nullable<int> ConfirmField;
        
        private System.Nullable<int> ErrCodeField;
        
        private string ErrDescField;
        
        private string HashField;
        
        private System.Nullable<decimal> MaxFeeField;
        
        private System.Nullable<decimal> MinFeeField;
        
        private string PinField;
        
        private System.Nullable<int> StateField;
        
        private string XmlField;
        
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
        public System.Nullable<decimal> Balance
        {
            get
            {
                return this.BalanceField;
            }
            set
            {
                this.BalanceField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.Nullable<int> Card
        {
            get
            {
                return this.CardField;
            }
            set
            {
                this.CardField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.Nullable<decimal> Commission
        {
            get
            {
                return this.CommissionField;
            }
            set
            {
                this.CommissionField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.Nullable<int> CommissionType
        {
            get
            {
                return this.CommissionTypeField;
            }
            set
            {
                this.CommissionTypeField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.Nullable<int> Confirm
        {
            get
            {
                return this.ConfirmField;
            }
            set
            {
                this.ConfirmField = value;
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
        public string Hash
        {
            get
            {
                return this.HashField;
            }
            set
            {
                this.HashField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.Nullable<decimal> MaxFee
        {
            get
            {
                return this.MaxFeeField;
            }
            set
            {
                this.MaxFeeField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.Nullable<decimal> MinFee
        {
            get
            {
                return this.MinFeeField;
            }
            set
            {
                this.MinFeeField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Pin
        {
            get
            {
                return this.PinField;
            }
            set
            {
                this.PinField = value;
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
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Xml
        {
            get
            {
                return this.XmlField;
            }
            set
            {
                this.XmlField = value;
            }
        }
    }
}


[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
[System.ServiceModel.ServiceContractAttribute(Namespace="api.regplat.ru", ConfigurationName="IXWcfApiService")]
public interface IXWcfApiService
{
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/SendPhoneCodeSMS", ReplyAction="api.regplat.ru/IXWcfApiService/SendPhoneCodeSMSResponse")]
    XWcfApiService.Result SendPhoneCodeSMS(int AppCode, string PhoneNumber, int CheckPhone, string UserIP);
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/NewUserRegistration", ReplyAction="api.regplat.ru/IXWcfApiService/NewUserRegistrationResponse")]
    XWcfApiService.Result NewUserRegistration(
                int AppCode, 
                string Phone, 
                System.Nullable<int> Code, 
                string Login, 
                string Password, 
                string LastName, 
                string FirstName, 
                string MiddleName, 
                System.Nullable<System.DateTime> BirthDate, 
                string AOGuidCityFias, 
                string City, 
                string AOGuidStreetFias, 
                string Street, 
                string AOGuidHouseFias, 
                string House, 
                string Flat, 
                string Email);
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/Test", ReplyAction="api.regplat.ru/IXWcfApiService/TestResponse")]
    [System.ServiceModel.FaultContractAttribute(typeof(XWcfApiService.Result), Action="https://apitest.regplat.ru/Result", Name="Result", Namespace="http://schemas.datacontract.org/2004/07/XWcfApiService")]
    XWcfApiService.Result Test(int AppCode, int k);
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/SetUserAccounts", ReplyAction="api.regplat.ru/IXWcfApiService/SetUserAccountsResponse")]
    XWcfApiService.Result SetUserAccounts(int AppCode, string Login, string Email, string Phone, string Password, string ExternAccount);
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/SendText", ReplyAction="api.regplat.ru/IXWcfApiService/SendTextResponse")]
    XWcfApiService.Result SendText(int AppCode, string From, string To, string Text);
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/SendTextMass", ReplyAction="api.regplat.ru/IXWcfApiService/SendTextMassResponse")]
    XWcfApiService.Result SendTextMass(int AppCode, string From, string Text, string[] To);
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/GetSections", ReplyAction="api.regplat.ru/IXWcfApiService/GetSectionsResponse")]
    XWcfApiService.Result GetSections(int AppCode, int Parent);
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/GetTemplatesInSection", ReplyAction="api.regplat.ru/IXWcfApiService/GetTemplatesInSectionResponse")]
    XWcfApiService.Result GetTemplatesInSection(int AppCode, int section_id);
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/GetAllSectionsAndTemplates", ReplyAction="api.regplat.ru/IXWcfApiService/GetAllSectionsAndTemplatesResponse")]
    XWcfApiService.Result GetAllSectionsAndTemplates(int AppCode, int Point_oid, bool Compression);
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/GetAllSectionsAndTemplatesHash", ReplyAction="api.regplat.ru/IXWcfApiService/GetAllSectionsAndTemplatesHashResponse")]
    XWcfApiService.Result GetAllSectionsAndTemplatesHash(int AppCode, int Point_oid);
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/GetCheckCardInfo", ReplyAction="api.regplat.ru/IXWcfApiService/GetCheckCardInfoResponse")]
    XWcfApiService.Result GetCheckCardInfo(int AppCode, int CardNumber, string PinNumber);
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/GetCommission", ReplyAction="api.regplat.ru/IXWcfApiService/GetCommissionResponse")]
    XWcfApiService.Result GetCommission(int AppCode, System.Nullable<int> Point_oid, int Template_tid, int Tempalte_sub_tid);
    
    [System.ServiceModel.OperationContractAttribute(Action="api.regplat.ru/IXWcfApiService/MakePrecheck", ReplyAction="api.regplat.ru/IXWcfApiService/MakePrecheckResponse")]
    XWcfApiService.Result MakePrecheck(int AppCode, bool CheckOnly, string SessionID, System.Nullable<int> PointOid, int Template_tid, int Template_sub_tid, string[] FieldNames, string[] FieldValues);
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
    
    public XWcfApiService.Result SendPhoneCodeSMS(int AppCode, string PhoneNumber, int CheckPhone, string UserIP)
    {
        return base.Channel.SendPhoneCodeSMS(AppCode, PhoneNumber, CheckPhone, UserIP);
    }
    
    public XWcfApiService.Result NewUserRegistration(
                int AppCode, 
                string Phone, 
                System.Nullable<int> Code, 
                string Login, 
                string Password, 
                string LastName, 
                string FirstName, 
                string MiddleName, 
                System.Nullable<System.DateTime> BirthDate, 
                string AOGuidCityFias, 
                string City, 
                string AOGuidStreetFias, 
                string Street, 
                string AOGuidHouseFias, 
                string House, 
                string Flat, 
                string Email)
    {
        return base.Channel.NewUserRegistration(AppCode, Phone, Code, Login, Password, LastName, FirstName, MiddleName, BirthDate, AOGuidCityFias, City, AOGuidStreetFias, Street, AOGuidHouseFias, House, Flat, Email);
    }
    
    public XWcfApiService.Result Test(int AppCode, int k)
    {
        return base.Channel.Test(AppCode, k);
    }
    
    public XWcfApiService.Result SetUserAccounts(int AppCode, string Login, string Email, string Phone, string Password, string ExternAccount)
    {
        return base.Channel.SetUserAccounts(AppCode, Login, Email, Phone, Password, ExternAccount);
    }
    
    public XWcfApiService.Result SendText(int AppCode, string From, string To, string Text)
    {
        return base.Channel.SendText(AppCode, From, To, Text);
    }
    
    public XWcfApiService.Result SendTextMass(int AppCode, string From, string Text, string[] To)
    {
        return base.Channel.SendTextMass(AppCode, From, Text, To);
    }
    
    public XWcfApiService.Result GetSections(int AppCode, int Parent)
    {
        return base.Channel.GetSections(AppCode, Parent);
    }
    
    public XWcfApiService.Result GetTemplatesInSection(int AppCode, int section_id)
    {
        return base.Channel.GetTemplatesInSection(AppCode, section_id);
    }
    
    public XWcfApiService.Result GetAllSectionsAndTemplates(int AppCode, int Point_oid, bool Compression)
    {
        return base.Channel.GetAllSectionsAndTemplates(AppCode, Point_oid, Compression);
    }
    
    public XWcfApiService.Result GetAllSectionsAndTemplatesHash(int AppCode, int Point_oid)
    {
        return base.Channel.GetAllSectionsAndTemplatesHash(AppCode, Point_oid);
    }
    
    public XWcfApiService.Result GetCheckCardInfo(int AppCode, int CardNumber, string PinNumber)
    {
        return base.Channel.GetCheckCardInfo(AppCode, CardNumber, PinNumber);
    }
    
    public XWcfApiService.Result GetCommission(int AppCode, System.Nullable<int> Point_oid, int Template_tid, int Tempalte_sub_tid)
    {
        return base.Channel.GetCommission(AppCode, Point_oid, Template_tid, Tempalte_sub_tid);
    }
    
    public XWcfApiService.Result MakePrecheck(int AppCode, bool CheckOnly, string SessionID, System.Nullable<int> PointOid, int Template_tid, int Template_sub_tid, string[] FieldNames, string[] FieldValues)
    {
        return base.Channel.MakePrecheck(AppCode, CheckOnly, SessionID, PointOid, Template_tid, Template_sub_tid, FieldNames, FieldValues);
    }
}
