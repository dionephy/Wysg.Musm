SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [app].[account](
	[account_id] [bigint] IDENTITY(1,1) NOT NULL,
	[uid] [nvarchar](100) NOT NULL,
	[email] [nvarchar](320) NOT NULL,
	[display_name] [nvarchar](200) NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[last_login_at] [datetime2](3) NULL
) ON [PRIMARY]
GO
ALTER TABLE [app].[account] ADD PRIMARY KEY CLUSTERED 
(
	[account_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
ALTER TABLE [app].[account] ADD  CONSTRAINT [UQ_account_email] UNIQUE NONCLUSTERED 
(
	[email] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
ALTER TABLE [app].[account] ADD  CONSTRAINT [UQ_account_uid] UNIQUE NONCLUSTERED 
(
	[uid] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [app].[account] ADD  CONSTRAINT [DF_account_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [app].[account] ADD  CONSTRAINT [DF_account_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO



SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [radium].[phrase](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[account_id] [bigint] NOT NULL,
	[text] [nvarchar](400) NOT NULL,
	[active] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[updated_at] [datetime2](3) NOT NULL,
	[rev] [bigint] NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [radium].[phrase] ADD PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
ALTER TABLE [radium].[phrase] ADD  CONSTRAINT [UQ_phrase_account_text] UNIQUE NONCLUSTERED 
(
	[account_id] ASC,
	[text] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_phrase_account_active] ON [radium].[phrase]
(
	[account_id] ASC,
	[active] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_phrase_account_rev] ON [radium].[phrase]
(
	[account_id] ASC,
	[rev] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [radium].[phrase] ADD  CONSTRAINT [DF_phrase_active]  DEFAULT ((1)) FOR [active]
GO
ALTER TABLE [radium].[phrase] ADD  CONSTRAINT [DF_phrase_created]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [radium].[phrase] ADD  CONSTRAINT [DF_phrase_updated]  DEFAULT (sysutcdatetime()) FOR [updated_at]
GO
ALTER TABLE [radium].[phrase] ADD  CONSTRAINT [DF_phrase_rev]  DEFAULT ((1)) FOR [rev]
GO
ALTER TABLE [radium].[phrase]  WITH CHECK ADD  CONSTRAINT [FK_phrase_account] FOREIGN KEY([account_id])
REFERENCES [app].[account] ([account_id])
ON DELETE CASCADE
GO
ALTER TABLE [radium].[phrase] CHECK CONSTRAINT [FK_phrase_account]
GO
ALTER TABLE [radium].[phrase]  WITH CHECK ADD  CONSTRAINT [CK_phrase_text_not_blank] CHECK  ((len(ltrim(rtrim([text])))>(0)))
GO
ALTER TABLE [radium].[phrase] CHECK CONSTRAINT [CK_phrase_text_not_blank]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [radium].[trg_phrase_touch] ON [radium].[phrase] AFTER UPDATE AS BEGIN SET NOCOUNT ON; ;WITH Changed AS ( SELECT i.id FROM inserted i JOIN deleted d ON i.id = d.id WHERE ( (i.active <> d.active) OR (i.[text] <> d.[text]) ) ) UPDATE p SET p.updated_at = SYSUTCDATETIME(), p.rev = p.rev + 1 FROM radium.phrase p INNER JOIN Changed c ON p.id = c.id; END; 
GO
ALTER TABLE [radium].[phrase] ENABLE TRIGGER [trg_phrase_touch]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [radium].[reportify_setting](
	[account_id] [bigint] NOT NULL,
	[settings_json] [nvarchar](max) NOT NULL,
	[updated_at] [datetime2](3) NOT NULL,
	[rev] [bigint] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [radium].[reportify_setting] ADD PRIMARY KEY CLUSTERED 
(
	[account_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_reportify_updated_at] ON [radium].[reportify_setting]
(
	[updated_at] DESC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [radium].[reportify_setting] ADD  CONSTRAINT [DF_reportify_updated]  DEFAULT (sysutcdatetime()) FOR [updated_at]
GO
ALTER TABLE [radium].[reportify_setting] ADD  CONSTRAINT [DF_reportify_rev]  DEFAULT ((1)) FOR [rev]
GO
ALTER TABLE [radium].[reportify_setting]  WITH CHECK ADD  CONSTRAINT [FK_reportify_account] FOREIGN KEY([account_id])
REFERENCES [app].[account] ([account_id])
ON DELETE CASCADE
GO
ALTER TABLE [radium].[reportify_setting] CHECK CONSTRAINT [FK_reportify_account]
GO




SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [radium].[hotkey](
	[hotkey_id] [bigint] IDENTITY(1,1) NOT NULL,
	[account_id] [bigint] NOT NULL,
	[trigger_text] [nvarchar](64) NOT NULL,
	[expansion_text] [nvarchar](4000) NOT NULL,
	[description] [nvarchar](256) NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[updated_at] [datetime2](3) NOT NULL,
	[rev] [bigint] NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [radium].[hotkey] ADD  CONSTRAINT [PK_hotkey] PRIMARY KEY CLUSTERED 
(
	[hotkey_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
ALTER TABLE [radium].[hotkey] ADD  CONSTRAINT [UQ_hotkey_account_trigger] UNIQUE NONCLUSTERED 
(
	[account_id] ASC,
	[trigger_text] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_hotkey_account_active] ON [radium].[hotkey]
(
	[account_id] ASC,
	[is_active] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_hotkey_account_rev] ON [radium].[hotkey]
(
	[account_id] ASC,
	[rev] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE NONCLUSTERED INDEX [IX_hotkey_account_trigger_active] ON [radium].[hotkey]
(
	[account_id] ASC,
	[trigger_text] ASC,
	[is_active] ASC
)
INCLUDE([expansion_text],[description]) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [radium].[hotkey] ADD  CONSTRAINT [DF_hotkey_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [radium].[hotkey] ADD  CONSTRAINT [DF_hotkey_created]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [radium].[hotkey] ADD  CONSTRAINT [DF_hotkey_updated]  DEFAULT (sysutcdatetime()) FOR [updated_at]
GO
ALTER TABLE [radium].[hotkey] ADD  CONSTRAINT [DF_hotkey_rev]  DEFAULT ((1)) FOR [rev]
GO
ALTER TABLE [radium].[hotkey]  WITH CHECK ADD  CONSTRAINT [FK_hotkey_account] FOREIGN KEY([account_id])
REFERENCES [app].[account] ([account_id])
ON DELETE CASCADE
GO
ALTER TABLE [radium].[hotkey] CHECK CONSTRAINT [FK_hotkey_account]
GO
ALTER TABLE [radium].[hotkey]  WITH CHECK ADD  CONSTRAINT [CK_hotkey_expansion_not_blank] CHECK  ((len(ltrim(rtrim([expansion_text])))>(0)))
GO
ALTER TABLE [radium].[hotkey] CHECK CONSTRAINT [CK_hotkey_expansion_not_blank]
GO
ALTER TABLE [radium].[hotkey]  WITH CHECK ADD  CONSTRAINT [CK_hotkey_trigger_not_blank] CHECK  ((len(ltrim(rtrim([trigger_text])))>(0)))
GO
ALTER TABLE [radium].[hotkey] CHECK CONSTRAINT [CK_hotkey_trigger_not_blank]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Trigger to auto-update updated_at and rev on meaningful changes
CREATE TRIGGER [radium].[trg_hotkey_touch] ON [radium].[hotkey]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH Changed AS (
        SELECT i.hotkey_id
        FROM inserted i
        JOIN deleted d ON i.hotkey_id = d.hotkey_id
        WHERE (i.is_active <> d.is_active) 
           OR (i.trigger_text <> d.trigger_text)
           OR (i.expansion_text <> d.expansion_text)
           OR (ISNULL(i.description,N'') <> ISNULL(d.description,N''))
    )
    UPDATE h
    SET h.updated_at = SYSUTCDATETIME(),
        h.rev = h.rev + 1
    FROM radium.hotkey h
    INNER JOIN Changed c ON h.hotkey_id = c.hotkey_id;
END;
GO
ALTER TABLE [radium].[hotkey] ENABLE TRIGGER [trg_hotkey_touch]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
ALTER TABLE [radium].[snippet] ADD  CONSTRAINT [PK_snippet] PRIMARY KEY CLUSTERED 
(
	[snippet_id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
ALTER TABLE [radium].[snippet] ADD  CONSTRAINT [UQ_snippet_account_trigger] UNIQUE NONCLUSTERED 
(
	[account_id] ASC,
	[trigger_text] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_snippet_account_active] ON [radium].[snippet]
(
	[account_id] ASC,
	[is_active] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_snippet_account_rev] ON [radium].[snippet]
(
	[account_id] ASC,
	[rev] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
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
ALTER TABLE [radium].[snippet] ADD  CONSTRAINT [DF_snippet_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [radium].[snippet] ADD  CONSTRAINT [DF_snippet_created]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [radium].[snippet] ADD  CONSTRAINT [DF_snippet_updated]  DEFAULT (sysutcdatetime()) FOR [updated_at]
GO
ALTER TABLE [radium].[snippet] ADD  CONSTRAINT [DF_snippet_rev]  DEFAULT ((1)) FOR [rev]
GO
ALTER TABLE [radium].[snippet]  WITH CHECK ADD  CONSTRAINT [FK_snippet_account] FOREIGN KEY([account_id])
REFERENCES [app].[account] ([account_id])
ON DELETE CASCADE
GO
ALTER TABLE [radium].[snippet] CHECK CONSTRAINT [FK_snippet_account]
GO
ALTER TABLE [radium].[snippet]  WITH CHECK ADD  CONSTRAINT [CK_snippet_trigger_not_blank] CHECK  ((len(ltrim(rtrim([trigger_text])))>(0)))
GO
ALTER TABLE [radium].[snippet] CHECK CONSTRAINT [CK_snippet_trigger_not_blank]
GO
ALTER TABLE [radium].[snippet]  WITH CHECK ADD  CONSTRAINT [CK_snippet_text_not_blank] CHECK  ((len(ltrim(rtrim([snippet_text])))>(0)))
GO
ALTER TABLE [radium].[snippet] CHECK CONSTRAINT [CK_snippet_text_not_blank]
GO
ALTER TABLE [radium].[snippet]  WITH CHECK ADD  CONSTRAINT [CK_snippet_ast_not_blank] CHECK  ((len(ltrim(rtrim([snippet_ast])))>(0)))
GO
ALTER TABLE [radium].[snippet] CHECK CONSTRAINT [CK_snippet_ast_not_blank]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Trigger to auto-update updated_at and rev on meaningful changes
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
