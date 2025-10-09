-- Add description column to radium.hotkey
ALTER TABLE [radium].[hotkey]
ADD [description] NVARCHAR(256) NULL;
GO

-- Optional: backfill description with first line of expansion_text initially
UPDATE h
SET h.[description] = CASE
    WHEN CHARINDEX(CHAR(10), h.expansion_text) > 0 THEN LEFT(h.expansion_text, CHARINDEX(CHAR(10), h.expansion_text) - 1)
    ELSE h.expansion_text
END
FROM radium.hotkey h
WHERE h.[description] IS NULL;
GO

-- Keep trigger intact (no change needed); future code uses description if present.
