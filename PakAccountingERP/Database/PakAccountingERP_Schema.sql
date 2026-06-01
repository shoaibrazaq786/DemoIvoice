-- Pak Accounting ERP - Full Database Schema
-- SQL Server | decimal(18,2) for financial values | Soft delete support

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'PakAccountingERP')
    CREATE DATABASE PakAccountingERP;
GO

USE PakAccountingERP;
GO

-- ASP.NET Identity tables are created via EF Core Migrations
-- This script supplements with stored procedures and indexes

-- Stored Procedure: Get Customer Ledger
CREATE OR ALTER PROCEDURE sp_GetCustomerLedger
    @CustomerId INT,
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT si.InvoiceDate AS [Date], si.InvoiceNumber AS Reference,
           si.NetTotal AS Debit, 0 AS Credit, 'Invoice' AS Type
    FROM SalesInvoices si
    WHERE si.CustomerId = @CustomerId AND si.CompanyId = @CompanyId
      AND si.IsPosted = 1 AND si.IsDeleted = 0
    ORDER BY si.InvoiceDate;
END
GO

-- Stored Procedure: Get Vendor Ledger
CREATE OR ALTER PROCEDURE sp_GetVendorLedger
    @VendorId INT,
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT b.BillDate AS [Date], b.BillNumber AS Reference,
           0 AS Debit, b.NetAmount AS Credit, 'Bill' AS Type
    FROM Bills b
    WHERE b.VendorId = @VendorId AND b.CompanyId = @CompanyId
      AND b.IsPosted = 1 AND b.IsDeleted = 0
    ORDER BY b.BillDate;
END
GO

-- Stored Procedure: Stock Valuation Report
CREATE OR ALTER PROCEDURE sp_GetStockValuation
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT i.ItemCode, i.ItemName, i.CurrentStock, i.PurchaseRate,
           (i.CurrentStock * i.PurchaseRate) AS Valuation
    FROM Items i
    WHERE i.CompanyId = @CompanyId AND i.IsDeleted = 0 AND i.ItemType = 1
    ORDER BY i.ItemName;
END
GO

-- Stored Procedure: Trial Balance
CREATE OR ALTER PROCEDURE sp_GetTrialBalance
    @CompanyId INT,
    @AsOfDate DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SELECT coa.AccountNumber, coa.AccountName,
           SUM(jel.Debit) AS TotalDebit, SUM(jel.Credit) AS TotalCredit
    FROM JournalEntryLines jel
    INNER JOIN JournalEntries je ON jel.JournalEntryId = je.Id
    INNER JOIN ChartOfAccounts coa ON jel.ChartOfAccountId = coa.Id
    WHERE je.CompanyId = @CompanyId AND je.IsPosted = 1 AND je.EntryDate <= @AsOfDate
      AND je.IsDeleted = 0
    GROUP BY coa.AccountNumber, coa.AccountName
    ORDER BY coa.AccountNumber;
END
GO

-- Stored Procedure: Daily Sales Report
CREATE OR ALTER PROCEDURE sp_GetDailySales
    @CompanyId INT,
    @ReportDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT si.InvoiceNumber, c.BuyerName, si.NetTotal, si.TaxAmount, si.InvoiceDate
    FROM SalesInvoices si
    INNER JOIN Customers c ON si.CustomerId = c.Id
    WHERE si.CompanyId = @CompanyId
      AND CAST(si.InvoiceDate AS DATE) = @ReportDate
      AND si.IsPosted = 1 AND si.IsDeleted = 0;
END
GO

-- Stored Procedure: Low Stock Alert
CREATE OR ALTER PROCEDURE sp_GetLowStockItems
    @CompanyId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT i.ItemCode, i.ItemName, i.CurrentStock, i.ReorderLevel, i.MinimumStock
    FROM Items i
    WHERE i.CompanyId = @CompanyId AND i.CurrentStock <= i.ReorderLevel AND i.IsDeleted = 0;
END
GO

-- Stored Procedure: Database Backup
CREATE OR ALTER PROCEDURE sp_BackupDatabase
    @BackupPath NVARCHAR(500)
AS
BEGIN
    DECLARE @DbName NVARCHAR(128) = DB_NAME();
    DECLARE @Sql NVARCHAR(1000) = N'BACKUP DATABASE [' + @DbName + N'] TO DISK = ''' + @BackupPath + N''' WITH FORMAT, INIT';
    EXEC sp_executesql @Sql;
END
GO

-- Performance Indexes (supplement EF migrations)
-- CREATE NONCLUSTERED INDEX IX_SalesInvoices_CompanyDate ON SalesInvoices(CompanyId, InvoiceDate) WHERE IsDeleted = 0;
-- CREATE NONCLUSTERED INDEX IX_InventoryTransactions_ItemDate ON InventoryTransactions(ItemId, TransactionDate);

PRINT 'Pak Accounting ERP stored procedures created successfully.';
GO
