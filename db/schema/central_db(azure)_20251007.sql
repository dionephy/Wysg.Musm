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


