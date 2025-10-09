-- Migration: Add snippet table to radium schema
-- Date: 2025-10-10
-- Description: Creates the snippet table for text expansion with AST support

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Create snippet table
CREATE TABLE [radium].[snippet](
	[snippet_id] [bigint] IDENTITY(1,1) NOT NULL,
	[account_id] [bigint] NOT NULL,
	[trigger_text] [nvarchar](64) NOT NULL,
	[snippet_text] [nvarchar](4000) NOT NULL,
	[snippet_ast] [nvarchar](max) NOT NULL,
	[description] [nvarchar](256) NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[updated_at] [datetime2](3) NOT NULL,
	[rev] [bigint] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

-- Add primary key
ALTER TABLE [radium].[snippet] ADD CONSTRAINT [PK_snippet] PRIMARY KEY CLUSTERED 
(
	[snippet_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

-- Add unique constraint on account_id + trigger_text
SET ANSI_PADDING ON
GO
ALTER TABLE [radium].[snippet] ADD CONSTRAINT [UQ_snippet_account_trigger] UNIQUE NONCLUSTERED 
(
	[account_id] ASC,
	[trigger_text] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

-- Add index on account_id + is_active for filtering active snippets
CREATE NONCLUSTERED INDEX [IX_snippet_account_active] ON [radium].[snippet]
(
	[account_id] ASC,
	[is_active] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

-- Add index on account_id + rev for sync operations
CREATE NONCLUSTERED INDEX [IX_snippet_account_rev] ON [radium].[snippet]
(
	[account_id] ASC,
	[rev] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

-- Add covering index for common lookup pattern
SET ANSI_PADDING ON
GO
CREATE NONCLUSTERED INDEX [IX_snippet_account_trigger_active] ON [radium].[snippet]
(
	[account_id] ASC,
	[trigger_text] ASC,
	[is_active] ASC
)
INCLUDE([snippet_text],[snippet_ast],[description]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

-- Add default constraints
ALTER TABLE [radium].[snippet] ADD CONSTRAINT [DF_snippet_active] DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [radium].[snippet] ADD CONSTRAINT [DF_snippet_created] DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [radium].[snippet] ADD CONSTRAINT [DF_snippet_updated] DEFAULT (sysutcdatetime()) FOR [updated_at]
GO
ALTER TABLE [radium].[snippet] ADD CONSTRAINT [DF_snippet_rev] DEFAULT ((1)) FOR [rev]
GO

-- Add foreign key constraint
ALTER TABLE [radium].[snippet] WITH CHECK ADD CONSTRAINT [FK_snippet_account] FOREIGN KEY([account_id])
REFERENCES [app].[account] ([account_id])
ON DELETE CASCADE
GO
ALTER TABLE [radium].[snippet] CHECK CONSTRAINT [FK_snippet_account]
GO

-- Add check constraint for trigger_text not blank
ALTER TABLE [radium].[snippet] WITH CHECK ADD CONSTRAINT [CK_snippet_trigger_not_blank] CHECK ((len(ltrim(rtrim([trigger_text])))>(0)))
GO
ALTER TABLE [radium].[snippet] CHECK CONSTRAINT [CK_snippet_trigger_not_blank]
GO

-- Add check constraint for snippet_text not blank
ALTER TABLE [radium].[snippet] WITH CHECK ADD CONSTRAINT [CK_snippet_text_not_blank] CHECK ((len(ltrim(rtrim([snippet_text])))>(0)))
GO
ALTER TABLE [radium].[snippet] CHECK CONSTRAINT [CK_snippet_text_not_blank]
GO

-- Add check constraint for snippet_ast not blank
ALTER TABLE [radium].[snippet] WITH CHECK ADD CONSTRAINT [CK_snippet_ast_not_blank] CHECK ((len(ltrim(rtrim([snippet_ast])))>(0)))
GO
ALTER TABLE [radium].[snippet] CHECK CONSTRAINT [CK_snippet_ast_not_blank]
GO

-- Create trigger to auto-update updated_at and rev on meaningful changes
CREATE TRIGGER [radium].[trg_snippet_touch] ON [radium].[snippet]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH Changed AS (
        SELECT i.snippet_id
        FROM inserted i
        JOIN deleted d ON i.snippet_id = d.snippet_id
        WHERE (i.is_active <> d.is_active) 
           OR (i.trigger_text <> d.trigger_text)
           OR (i.snippet_text <> d.snippet_text)
           OR (i.snippet_ast <> d.snippet_ast)
           OR (ISNULL(i.description,N'') <> ISNULL(d.description,N''))
    )
    UPDATE s
    SET s.updated_at = SYSUTCDATETIME(),
        s.rev = s.rev + 1
    FROM radium.snippet s
    INNER JOIN Changed c ON s.snippet_id = c.snippet_id;
END;
GO
ALTER TABLE [radium].[snippet] ENABLE TRIGGER [trg_snippet_touch]
GO
