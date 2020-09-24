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
                o.paymentMethod == null || o.mainDescription == null || o.secondaryDescription == null)
                {
                    Console.WriteLine("Not enough arguments given (name, phone, price, pay method, main description, secondary description)");
                    Console.ReadKey();
                    return;
                }
                result = (int)sendInvoiceToCustomer(o);
            });

            return result;
        }

        static private long? sendInvoiceToCustomer(CommandLineOptions options)
        {
            Accounting_Documents_Create_Request invoiceCreateRequest = createInvoiceDocument(options);
            Response_Accounting_Documents_Create_Response createResponse = apiClient.AccountingDocumentsCreateAsync(invoiceCreateRequest).Result;
            if (createResponse.Status != ResponseStatus.Success)
            {
                Console.WriteLine("Failed creating invoice: " + createResponse.UserErrorMessage);
                return -1;
            }

            Console.WriteLine("Created invoice successfully!");
            if (options.customerEmail == null)
            {
                Console.WriteLine("No customer email specified, not sending document.");
                return 0;
            }

            long craetedInvoiceID = createResponse.Data.DocumentID;

            Accounting_Documents_Send_Request send_Request = generateSendDocumentRequest(craetedInvoiceID, options);
            Core_APIEmptyResponse sendResponse = apiClient.AccountingDocumentsSendAsync(send_Request).Result;

            if (sendResponse.Status != ResponseStatus.Success)
            {
                Console.WriteLine("Failed sending invoice: " + createResponse.UserErrorMessage);
                return -1;
            }
            else
            {
                Console.WriteLine("Sent invoice successfully!");
            }

            if (options.isDraft)
            {
                Console.WriteLine("Invoice is draft, not returning the invoice id");
                return 0;
            }

            return createResponse.Data.DocumentNumber;
        }

        static private Accounting_Documents_Send_Request generateSendDocumentRequest(long documentID, CommandLineOptions options)
        {
            return new Accounting_Documents_Send_Request()
            {
                Credentials = generateGarageSaleCredentials(),
                EntityID = documentID,
                DocumentType = Accounting_Typed_DocumentType.InvoiceAndReceipt,
                EmailAddress = options.customerEmail,
                Original = true,
                Language = getPreferredLanguage(options.preferredLanguage)
            };
        }

        static private Accounting_Documents_Create_Request createInvoiceDocument(CommandLineOptions options)
        {
            return new Accounting_Documents_Create_Request()
            {
                Details = generateDocumentDetails(options),
                Items = new List<Accounting_Typed_DocumentItem>() { generateItem(options.amount, options.mainDescription, options.secondaryDescription) },
                Payments = new List<Accounting_Typed_DocumentPayment> { generatePayment(options.amount, options.paymentMethod) },
                VATIncluded = true,
                Credentials = generateGarageSaleCredentials()
            };
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
                SendByEmail = generateSendByEmail(options.customerEmail),
                Language = getPreferredLanguage(options.preferredLanguage),
                Currency = Accounting_Typed_DocumentCurrency.ILS,
                Type = Accounting_Typed_DocumentType.InvoiceAndReceipt
            };
            
            if(options.messageForCustomer != "")
            {
                documentDetails.Description = options.messageForCustomer;
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
