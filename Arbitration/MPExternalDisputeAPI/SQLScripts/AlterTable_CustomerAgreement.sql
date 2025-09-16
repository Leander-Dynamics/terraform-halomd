-- use arbit database

ALTER TABLE Customers
ADD AgreementStartDate Datetime2(7) NULL;

GO

ALTER TABLE Customers
ADD AgreementEndDate Datetime2(7) NULL;

GO 

ALTER TABLE Customers
ADD ExternalPartnerName nvarchar(127) NULL;
