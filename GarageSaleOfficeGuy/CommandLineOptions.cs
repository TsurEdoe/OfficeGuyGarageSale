﻿using CommandLine;

namespace GarageSaleOfficeGuy
{
    class CommandLineOptions
    {
        [Option('t', "tourism", HelpText = "Use this option to send a tourism invoice ().")]
        public bool isTourismInvoice { get; set; }

        [Option('A', "amount", HelpText = "Amount Customer paid.")]
        public float? amount { get; set; }

        [Option('P', "payment", HelpText = "Payment method (Cash, Credit, BankTransfer, Bit, PayBox, Cheque")]
        public PaymentMethod? paymentMethod { get; set; }

        [Option('n', "name", HelpText = "Customer name.")]
        public string customerName { get; set; }

        [Option('p', "phone", HelpText = "Customer phone number.")]
        public string customerPhoneNumer { get; set; }

        [Option('e', "email", HelpText = "Customer email.")]
        public string customerEmail { get; set; }

        [Option('c', "city", HelpText = "Customer city")]
        public string customerCity { get; set; }

        [Option('a', "address", HelpText = "Customer address")]
        public string customerAddress{ get; set; }

        [Option('v', "vatFree", HelpText = "Use this option if the customer is VAT free customer pay VAT.")]
        public bool vatFree { get; set; }

        [Option('l', "preferredLanguage", Default = "hebrew", HelpText = "Preferred language to display invoice (hebrew/arabic/english/spanish).")]
        public string preferredLanguage { get; set; }

        [Option('m', "messageForCustomer", Default = "", HelpText = "The description for the invoice.")]
        public string messageForCustomer { get; set; }

        [Option('d', "isDraft", HelpText = "Use this option to create a draft document")]
        public bool isDraft { get; set; }

        [Option('i', "items", Default = "", HelpText = "Items for the invoice.")]
        public string items { get; set; }

        [Option('s', "sendDocument", HelpText = "Should the invoice be sent.")]
        public bool sendDocument{ get; set; }

        [Option('f', "finalizeDocument", Default = -1 ,HelpText = "Finalize a draft invoice.")]
        public long finalizeDocument { get; set; }

        [Option('w', "sendWhatsapp", Default = "", HelpText = "Send a WhatsApp message")]
        public string sendWhatsapp { get; set; }
    }
}
