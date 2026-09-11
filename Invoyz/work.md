Act as a senior software developer and help me to create a component diagram for my invoice application. i want to use PlantUML.

I want to follow the clean architecture where my outter layer will be:

infraesctructure layer where we will have the UI and the database.  

Controller layer where we will have controlers for each resource: customers, products, invoices.

application layer where we will have our services, create a service for each resource as well.

Domain layer where we will have our domain entities: customer, product, invoice, invoice line. 

I want to have a DTO for each domain entity that will be used by the applicaation and controller layer. 

Following you have the domain model with the 4 entities and their attributes, please generate me the contrains and validation rules you think are best for each one. I want to review them and accept each individually.

Customer
Id	System-generated, GUID, immutable, required
Name	Required, non-empty after trim, max length 200
Email	Required, valid email format (RFC 5322 regex), max length 254, unique across customers
Address	Required, max length 500
VatNumber Required, must match a valid VAT format (country-prefix + digits), unique

Invoices	Not a validated field — enforced via FK relationship (Invoice.CustomerId), not nullable

Product
Id	System-generated, GUID, immutable, required
Name	Required, non-empty, max length 200
Description	Optional, max length 1000
UnitPrice	Required >= 0, decimal 
TaxRate	Required  range 0–100

Invoice

Id	System-generated, GUID, immutable, required
InvoiceNumber Required, unique, max length 50
CustomerId	Required
IssueDate	Required, must be a valide date 
DueDate	Required, must be a valide date, must be >= IssueDate
Status	Required, enum (Draft, Sent, Paid, Overdue)
SubTotal	Required, >= 0
TotalTax	Required, >= 0
GrandTotal	Required, >= 0
Lines Not a validated field — enforced via FK relationship (InvoiceLine.InvoiceId), not nullable

InvoiceLine
Id	System-generated, GUID, immutable, required
InvoiceId	Required, must reference existing Invoice
ProductId	Required, must reference existing Product
Quantity	Required, > 0 integer
UnitPrice	Required, >= 0, decimal 
TaxRate	Required, range 0–100
LineTotal	Required, >= 0, decimal 
LineTax	Required, >= 0, decimal 

Please create a folder for each layer inside the server folder.

now please create a folder for each domain model with its attributes. I want to have a validator for each one that will verify the rules and contraints that we saw earlier.


Create the DTOs for each domain model entity. Note that a DTO must hide the business rules. 

Create the following 3 services inside the application layer:

CustomerService
ProductService
InvoiceService

Please dont write any more code, than needed. 

create the controller layer with the 4 controllers:

Customers, products, invoices, invoiceLines. 

For each one, please create its own file, with the following operations: GET, POST, PUT, PATCH, DELETE. 


Regarding the infrastructure layer I want to deal with that only on cache as a mock of a real database. 


I want a very clean minimal and responsive UI. 
Regarding technology: We are using vue.js and for styling lets use Tailwind. For API connection lets use Axios.

For now, dont make any connection with the backend via HTTP requests, first I just want to feel the direction you are going so we can work first strctily UI matters. 

The main areas are to manage customers, products and invocies.
In each area, I want to be able to list, add new, update and delete entires via forms. 

Regarding the invoices, I want to be able to see their details and calculated totals, also to expand themn and see their individual lines.


Now I want to connect the frontend with the backend. I want to do it step by step. First lets do the customer. I want that all CRUD operations on the frotnend to connect to the backend. You can remove the mock data from the frontend since now we are using the real data from the backend. 


--- TESTS ---


Create a test solution for the server side that will mirror the structure of our project so we can eventually test everything, from unit tests on the domain model to integration tests end to end. However, dont create any test for now, only the structure with the folders.


Create unit tests for the product domain models entity, testing its attributes against the business rules we defined. I want to have tested both positive scenarios as well as the negative ones, taking into consideration edge cases.

For the product logic, create a test suite for the api, mocking the service layer. I want to have tests for every operation on each endpoint. Testing sucess scenarios and also fail scenarios. 

For the product logic, create a test suite for application layer, mocking the service repository. I want to have tests for every operation on this layer. Testing sucess scenarios and also failing ones,  taking into consideration edge cases.

For the product logic, Create a test suite to test our server end to end, from the entry point in the controllers until the repository. Testing sucess scenarios and also fail scenarios, taking into consideration edge cases.

Now I want to do some integration tests, testing the application end to end, from the client to the server.



Button to print PDF -> request API endpoint GET /pdfInvoice/100 -> validate all needed info -> return result to the client 

while status is printing the button should be disabled 

{ 
    status : printing
    PDF : NULL
}

client will keep pulling GET /invoicePdf/100 - PRINTING

{ 
    status : printing
    PDF : NULL
}


client will keep pulling GET /invoicePdf/100 - PRINTING

{ 
    status : printed 
    PDF : PDF 
}


{ 
    status : cantPrint
    PDF : NULL
}



Now I want to have a PDF generator that is triggered by the frontend for each invoice. It will contain customer details, line items, and totals.

This should not freeze the frontend waiting for it, imagine that it is possible to have invoices with thousands or milions of lines which could take a lot of processing time.

My idea is to when the user clicks the PRINT button, the client will send a HTTP GET for /invoicePdf with the respective InvoiceID.

the backend should call a separated service that will start the PDF creation for that Invoice. Allowing the GET request to return to the client telling that is processing the invoice. Something like this: 

{ 
    status : printing
    PDF : NULL
}


The user should be able to freely navigate and edit the rest of the application. Can you see the possibility of errors if the user starts to edit things that will be on the pdf?  

My idea is for the frontend to have a repetitive pulling system for that /invoicePdf that will consult the status of the printing. 


Probably after some printing status, it will finally receive something like:

{ 
    status : printed 
    PDF : PDF 
}

And trigger the download of the invoice. 


What do you think about this approach, problems we could have, things we could fix?



component diagram.
domain model.
swagger.


I understand for simple math such as additions the logic could be on the frontend. But thinking on scaling this APP for the future, I want to transfer all computations to the backend, so the logic will live there and be easier to modify or add new features in the future. 

Take into consideration the tests you created for the frontend related to this, don't break anything and make sure that all the rest is working.

