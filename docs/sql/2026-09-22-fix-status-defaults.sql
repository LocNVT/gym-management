-- Corrects rows written before 2026-09-22, when the C# defaults disagreed with the
-- labels shown in the UI: a new member was born "Suspended", a new invoice "Paid",
-- a new subscription "Expired".
--
-- DO NOT RUN THIS BLIND. There is no way to tell from the data whether an invoice with
-- Status = 1 was genuinely paid or merely defaulted. Review the rows first, and only run
-- the statements that match what actually happened in your gym.
--
-- Always take a backup first.

-- 1. Review before changing anything.
SELECT Status, COUNT(*) AS Rows, MIN(CreatedAt) AS Earliest, MAX(CreatedAt) AS Latest
FROM Members GROUP BY Status;

SELECT Status, COUNT(*) AS Rows, SUM(TotalAmount) AS Total, MIN(InvoiceDate) AS Earliest
FROM Invoices GROUP BY Status;

SELECT Status, COUNT(*) AS Rows, MIN(CreatedAt) AS Earliest FROM MemberDataServices GROUP BY Status;

-- 2. Members: rows never deliberately suspended should be Active (0).
-- UPDATE Members SET Status = 0 WHERE Status = 1 AND IsDeleted = 0;

-- 3. Subscriptions: a subscription whose EndDate is still in the future was never
-- genuinely "Expired" — it only looked that way because of the bad default.
-- UPDATE MemberDataServices SET Status = 0 WHERE Status = 1 AND EndDate > GETUTCDATE();

-- 4. Invoices: deliberately left commented out. Rewriting payment status changes the
-- books. Decide invoice by invoice.
-- UPDATE Invoices SET Status = 0 WHERE Status = 1 AND Id IN (...);
