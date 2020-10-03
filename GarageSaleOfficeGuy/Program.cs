using System;
using System.Collections.Generic;
using CommandLine;
using OfficeGuy.APIs;

namespace GarageSaleOfficeGuy
{
    class Program
    {
        static OfficeGuyApiClient apiClient = new OfficeGuyApiClient();

        static int Main(string[] args)
        {
            int result = -1;
            Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed<CommandLineOptions>(o => 
            {
                if (o.customerName == null || o.customerPhoneNumer == null || o.amount == null || 
                o.paymentMethod == null || o.items == null)
                {
                    Console.WriteLine("Not enough arguments given (name, phone, price, pay method, main description, secondary description)");
                    Console.ReadKey();
                    return;
                }
                result = (int)createAndSendInvoiceToCustomer(o);
            });

            return result;
        }

        static private long? createAndSendInvoiceToCustomer(CommandLineOptions options)
        {
            if (options.isTourismInvoice)
            {
                if(!replaceLogo(true))
                {
                    Console.WriteLine("Failed replacing logo in website");
                    return -1;
                }
            }

            Accounting_Documents_Create_Request invoiceCreateRequest = createInvoiceDocument(options);
            Response_Accounting_Documents_Create_Response createResponse = apiClient.AccountingDocumentsCreateAsync(invoiceCreateRequest).Result;

            if (options.isTourismInvoice)
            {
                System.Threading.Thread.Sleep(5000);
                if (!replaceLogo(false))
                {
                    Console.WriteLine("Failed replacing logo in website");
                    return -1;
                }
            }

            if (createResponse.Status != ResponseStatus.Success)
            {
                Console.WriteLine("Failed creating invoice: " + createResponse.UserErrorMessage);
                return -1;
            }

            Console.WriteLine("Created invoice successfully!");

            long craetedInvoiceID = createResponse.Data.DocumentID;

            if (options.isDraft)
            {
                Console.WriteLine("Invoice is draft, returning the document id");
                return craetedInvoiceID;
            }
            else
            {
                Console.WriteLine("Sent invoice successfully!");
            }

            return createResponse.Data.DocumentNumber;
        }

        /**
         * If changeToTourism = true, changes to tourism logo. Else, changes to GarageSale logo
         */
        static private bool replaceLogo(bool changeToTourism)
        {
            string resourceName = changeToTourism ? "tourism" : "garage_sale";
            string invoiceTitle = changeToTourism ? "הנעה וליווי עסקים וקהילות בתחומי קיימות ותיירות" : "בחצר האחורית גראז' סייל - לשחרר ולאפשר לשפע להיכנס";
            Console.WriteLine("Replacing logo in website to " + resourceName + " logo");
            Console.WriteLine(Properties.Resources.ResourceManager.GetObject(resourceName).ToString().Length);
            Website_Companies_Update_Request request = new Website_Companies_Update_Request
            {
                Company = generateGarageSaleCompanyDetails(resourceName, invoiceTitle),
                Credentials = generateGarageSaleCredentials()
            };
            return apiClient.WebsiteCompaniesUpdateAsync(request).Result.Status == ResponseStatus.Success;
        }

        static private Accounting_Documents_Create_Request createInvoiceDocument(CommandLineOptions options)
        {
            return new Accounting_Documents_Create_Request()
            {
                Details = generateDocumentDetails(options),
                Items = generateItems(options.items),
                Payments = new List<Accounting_Typed_DocumentPayment> { generatePayment(options.amount, options.paymentMethod) },
                VATIncluded = true,
                Credentials = generateGarageSaleCredentials()
            };
        }

        static private List<Accounting_Typed_DocumentItem> generateItems(string itemsString)
        {
            List<Accounting_Typed_DocumentItem> itemList = new List<Accounting_Typed_DocumentItem>();
            if (itemsString == "")
            {
                itemList.Add(generateItem(0, "", ""));
                return itemList;
            }

            foreach (string element in itemsString.Split(';'))
            {
                string[] singleRowData = element.Split('!');
                if (singleRowData.Length != 3)
                {
                    continue;
                }
                
                float currentAmount = (singleRowData[0] == "") ? 0 : float.Parse(singleRowData[0]);
                itemList.Add(generateItem(currentAmount, singleRowData[1], singleRowData[2]));
            }

            return itemList;
        }

        static private Accounting_Typed_DocumentItem generateItem(float? amountPaid, string mainDescription, string secondaryDescription)
        {
            return new Accounting_Typed_DocumentItem()
            {
                Quantity = 1,
                UnitPrice = amountPaid,
                Description = secondaryDescription,
                TotalPrice = amountPaid,
                DocumentCurrency_TotalPrice = amountPaid,
                DocumentCurrency_UnitPrice = amountPaid,
                Item = new Accounting_Typed_IncomeItem()
                {
                    Cost = 0,
                    Currency = Accounting_Typed_DocumentCurrency.ILS,
                    Name = mainDescription,
                    Price = amountPaid,
                    SearchMode = Accounting_Typed_IncomeItemSearchMode.Automatic
                }
            };
        }
        static private Accounting_Typed_DocumentPayment generatePayment(float? amountPaid, PaymentMethod? payment)
        {
            Accounting_Typed_DocumentPayment paymentDoc = new Accounting_Typed_DocumentPayment()
            {
                Amount = amountPaid
            };

            switch(payment)
            {
                case PaymentMethod.Cash:
                    paymentDoc.Details_Cash = new Accounting_Typed_Payment_Cash();
                    break;
                case PaymentMethod.BankTransfer:
                    paymentDoc.Details_Other = new Accounting_Typed_Payment_Other()
                    {
                        Type = "העברה בנקאית"
                    };
                    break;
                case PaymentMethod.BIT:
                    paymentDoc.Details_Other = new Accounting_Typed_Payment_Other() 
                    {
                        Type = "Bit"
                    };
                    break;
                case PaymentMethod.PayBox:
                    paymentDoc.Details_Other = new Accounting_Typed_Payment_Other()
                    {
                        Type = "Paybox"
                    };
                    break;
                case PaymentMethod.PepperPay:
                    paymentDoc.Details_Other = new Accounting_Typed_Payment_Other()
                    {
                        Type = "PepperPay"
                    };
                    break;
                case PaymentMethod.Cheque:
                    paymentDoc.Details_Other = new Accounting_Typed_Payment_Other()
                    {
                        Type = "צ'ק"
                    };
                    break;
                default:
                    paymentDoc.Details_General = new Accounting_Typed_Payment_General();
                    break;
            }

            return paymentDoc;
        }

        static private Website_Typed_Company generateGarageSaleCompanyDetails(string resourceName, string invoiceTitle)
        {
            byte[] chosenLogo = (byte[])Properties.Resources.ResourceManager.GetObject(resourceName);
            
            return new Website_Typed_Company()
            {
                Name = "מאירה צור",
                EmailAddress = "tsur.meira@gmail.com",
                Country = "ישראל",
                Address = "מושב ציפורי",
                Phone = "050-6890998",
                Fax = null,
                Title = invoiceTitle,
                CorporateNumber = "028466563",
                English_Name = null,
                English_Address = null,
                English_Country = null,
                English_Phone = null,
                English_Fax = null,
                English_Title = null,
                CompanyType = CompanyType.LicensedDealer,
                Logo = chosenLogo,
                Website = null
            };
        }

        static private Core_APICredentials generateGarageSaleCredentials()
        {
            Core_APICredentials garageSaleCredentials = new Core_APICredentials();
            garageSaleCredentials.APIKey = "MRCqk4ZWSgXAiT2v50l9z8kB0t6wpyi7JM2U7ElepDxbX9ldKj";
            garageSaleCredentials.CompanyID = 72019085;
            return garageSaleCredentials;
        }
        static private Accounting_Typed_DocumentDetails generateDocumentDetails(CommandLineOptions options)
        {
            Accounting_Typed_DocumentDetails documentDetails = new Accounting_Typed_DocumentDetails() 
            {
                IsDraft = options.isDraft,
                Customer = generateCustomer(options.customerName, 
                                            options.customerPhoneNumer, 
                                            options.customerEmail,
                                            options.vatFree,
                                            options.customerCity,
                                            options.customerAddress),
                Language = getPreferredLanguage(options.preferredLanguage),
                Currency = Accounting_Typed_DocumentCurrency.ILS,
                Type = Accounting_Typed_DocumentType.InvoiceAndReceipt
            };
            
            if(options.messageForCustomer != "")
            {
                documentDetails.Description = options.messageForCustomer;
            }

            if(options.sendDocument)
            {
                documentDetails.SendByEmail = generateSendByEmail(options.customerEmail);
            }
           
            return documentDetails;

        }
        static private Accounting_Typed_Language getPreferredLanguage(string language)
        {
            switch(language)
            {
                case "hebrew":
                    return Accounting_Typed_Language.Hebrew;
                case "english":
                    return Accounting_Typed_Language.English;
                case "arabic":
                    return Accounting_Typed_Language.Arabic;
                case "spanish":
                    return Accounting_Typed_Language.Spanish;
                default:
                    return Accounting_Typed_Language.Hebrew;
            }
        }
        static private Accounting_Typed_Customer generateCustomer(string customerName, string customerPhoneNumer, string customerEmail, 
                                                                    bool vatFree, string customerCity, string customerAddress)
        {
            Accounting_Typed_Customer customer = new Accounting_Typed_Customer()
            {
                Name = customerName,
                Phone = customerPhoneNumer,
                EmailAddress = customerEmail,
                NoVAT = vatFree,
                SearchMode = 0
             };

            if (customerCity != null)
            {
                customer.City = customerCity;
            }

            if (customerAddress != null)
            {
                customer.Address = customerAddress;
            }

            return customer;
        }

        static private Accounting_Typed_DocumentSendByEmail generateSendByEmail(string email)
        {
            return new Accounting_Typed_DocumentSendByEmail()
            {
                EmailAddress = email,
                Original = true,
                SendAsPaymentRequest = false
            };
        }
    }
}
