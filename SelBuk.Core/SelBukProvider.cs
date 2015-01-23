using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using SelBuk.Core.com.selbuk.www;
using SelBuk.Core.Models;

namespace SelBuk.Core
{
    public class SelBukProvider
    {
        #region private variables

        string _userName;
        string _password;
        string _token;
        int m_MaxPage = 5;
        SelBuk.Core.com.selbuk.www.WebService_GetSalesService service = new SelBuk.Core.com.selbuk.www.WebService_GetSalesService();

        #endregion

        #region ctor

        public SelBukProvider(string userName, string password)
        {
            _userName = userName;
            _password = password;
        }

        #endregion

        public bool Authenticate()
        {
            _token = service.authenticate(_userName, _password).ToString().Replace("{", string.Empty).Replace("}", string.Empty);
            if (_token != "nvu" || _token != "1")
            {
                return true;
            }

            return false;
        }

        public List<Sales> SearchOrders(string orderId, DateTime? beginTime, DateTime? endTime, bool shipped)
        {
            string xml = string.Empty;
            List<Sales> sales = new List<Sales>();
            string ship = shipped ? "1" : "0";

            if (!String.IsNullOrEmpty(orderId))
            {
                Authenticate();
                xml = service.GetSales(_token, orderId);

                if (GetResultCode(xml) == "OK")
                {
                    StringReader _InXml = new StringReader(xml);
                    XmlSerializer _oxs = new XmlSerializer(typeof(SalesAdd));
                    SalesAdd order = (SalesAdd)_oxs.Deserialize(_InXml);
                    sales.AddRange(order.Items);
                    return sales;
                }

            }
            else
            {
                for (int i = 0; i <= m_MaxPage; i++)
                {
                    Authenticate();

                    xml = service.GetSalesByDateRange(beginTime.Value.ToString("yyyy-MM-dd HH':'mm':'ss"), endTime.Value.ToString("yyyy-MM-dd HH':'mm':'ss"), _token, ship, i);

                    if (GetResultCode(xml) == "OK")
                    {
                        StringReader _InXml = new StringReader(xml);
                        XmlSerializer _oxs = new XmlSerializer(typeof(SalesAdd));

                        SalesAdd order = null;

                        try
                        {
                            order = (SalesAdd)_oxs.Deserialize(_InXml);
                        }
                        catch (Exception)
                        {
                            continue;
                        }

                        if (order.Items.Count() == 0)
                            break;

                        sales.AddRange(order.Items);

                        if (order.Items.Count() < 100)
                            break;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return sales;

        }

        public List<Purchases> SearchPurchases(DateTime beginTime, DateTime endTime, string status)
        {
            //IsReceived,IsPurchased
            string xml = service.GetPurchasesByDateRange(beginTime.ToString("yyyy-MM-dd HH':'mm':'ss"), endTime.ToString("yyyy-MM-dd HH':'mm':'ss"), _token, "IsReceived", 0);

            if (GetResultCode(xml) == "OK")
            {
                StringReader _InXml = new StringReader(xml);
                XmlSerializer _oxs = new XmlSerializer(typeof(PurchaseAdd));
                PurchaseAdd order = (PurchaseAdd)_oxs.Deserialize(_InXml);
                return order.Items.ToList();
            }

            return new List<Purchases>();

        }

        public List<Sales> GetCancelledOrdersByDateRange(DateTime beginTime, DateTime endTime)
        {
            Authenticate();
            List<Sales> sales = new List<Sales>();
            string xml = service.GetCancelledOrdersByDateRange(beginTime.ToString("yyyy-MM-dd HH':'mm':'ss"), endTime.ToString("yyyy-MM-dd HH':'mm':'ss"), _token, 0);

            if (GetResultCode(xml) == "OK")
            {
                StringReader _InXml = new StringReader(xml);
                XmlSerializer _oxs = new XmlSerializer(typeof(SalesAdd));
                SalesAdd order = (SalesAdd)_oxs.Deserialize(_InXml);

                if (order != null)
                {
                    sales.AddRange(order.Items.ToList());
                }
            }

            return sales;

        }

        public List<Sales> GetReturnsByDateRange(DateTime beginTime, DateTime endTime)
        {
            Authenticate();
            List<Sales> sales = new List<Sales>();

            string xml = service.GetReturns(beginTime.ToString("yyyy-MM-dd HH':'mm':'ss"), endTime.ToString("yyyy-MM-dd HH':'mm':'ss"), _token, null, 0);
            if (GetResultCode(xml) == "OK")
            {
                StringReader _InXml = new StringReader(xml);
                XmlSerializer _oxs = new XmlSerializer(typeof(SalesAdd));
                SalesAdd order = (SalesAdd)_oxs.Deserialize(_InXml);

                if (order != null)
                {
                    sales.AddRange(order.Items);
                }
            }

            return sales;

        }

        public List<Payments> SearchPayments(DateTime beginTime, DateTime endTime)
        {
            List<Payments> payments = new List<Payments>();

            for (int i = 0; i <= m_MaxPage; i++)
            {
                Authenticate();
                string xml = service.GetPayments(beginTime.ToString("yyyy-MM-dd HH':'mm':'ss"), endTime.ToString("yyyy-MM-dd HH':'mm':'ss"), _token, null, i);

                if (GetResultCode(xml) == "OK")
                {
                    StringReader _InXml = new StringReader(xml);
                    XmlSerializer _oxs = new XmlSerializer(typeof(PaymentsAdd));

                    PaymentsAdd order = (PaymentsAdd)_oxs.Deserialize(_InXml);

                    payments.AddRange(order.Items);

                    if (order.Items.Count() < 100)
                        break;
                }
                else
                {
                    break;
                }
            }

            return payments;

        }

        public string UpdateInventory(string sku, decimal quantity)
        {
            Authenticate();
            string result = service.UpdateInventory(_token, sku, Convert.ToDouble(quantity));
            return GetResultCode(result);
        }


        #region private methods

        private string GetResultCode(string result)
        {
            switch (result)
            {
                case "0":
                    return "SKU or quantity not received";
                case "1":
                    return "bad authentication token";
                case "2":
                    return "token does not match";
                case "3":
                    return "no matching sku or orders found";
                case "4":
                    return "no orders in query";
                default:
                    return "OK";
            }
        }

        #endregion


    }
}
