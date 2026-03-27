using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventCalenderApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketsAndProfileImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add ProfileImageUrl to Users (only if column doesn't exist)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'ProfileImageUrl'
                )
                BEGIN
                    ALTER TABLE [Users] ADD [ProfileImageUrl] nvarchar(max) NULL;
                END
            ");

            // Add RefundCutoffDays to Events (only if column doesn't exist)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Events' AND COLUMN_NAME = 'RefundCutoffDays'
                )
                BEGIN
                    ALTER TABLE [Events] ADD [RefundCutoffDays] int NOT NULL DEFAULT 2;
                END
            ");

            // Add EarlyRefundPercentage to Events (only if column doesn't exist)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Events' AND COLUMN_NAME = 'EarlyRefundPercentage'
                )
                BEGIN
                    ALTER TABLE [Events] ADD [EarlyRefundPercentage] real NOT NULL DEFAULT 100.0;
                END
            ");

            // Create Tickets table (only if it doesn't exist)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_NAME = 'Tickets'
                )
                BEGIN
                    CREATE TABLE [Tickets] (
                        [TicketId]    int           NOT NULL IDENTITY,
                        [UserId]      int           NOT NULL,
                        [EventId]     int           NOT NULL,
                        [PaymentId]   int           NULL,
                        [GeneratedAt] datetime2     NOT NULL,
                        CONSTRAINT [PK_Tickets] PRIMARY KEY ([TicketId]),
                        CONSTRAINT [FK_Tickets_Users_UserId]
                            FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId])
                            ON DELETE CASCADE,
                        CONSTRAINT [FK_Tickets_Events_EventId]
                            FOREIGN KEY ([EventId]) REFERENCES [Events] ([EventId])
                            ON DELETE CASCADE,
                        CONSTRAINT [FK_Tickets_Payments_PaymentId]
                            FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([PaymentId])
                            ON DELETE NO ACTION
                    );

                    CREATE INDEX [IX_Tickets_UserId]    ON [Tickets] ([UserId]);
                    CREATE INDEX [IX_Tickets_EventId]   ON [Tickets] ([EventId]);
                    CREATE INDEX [IX_Tickets_PaymentId] ON [Tickets] ([PaymentId]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Tickets')
                    DROP TABLE [Tickets];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Events' AND COLUMN_NAME = 'EarlyRefundPercentage')
                    ALTER TABLE [Events] DROP COLUMN [EarlyRefundPercentage];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Events' AND COLUMN_NAME = 'RefundCutoffDays')
                    ALTER TABLE [Events] DROP COLUMN [RefundCutoffDays];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'ProfileImageUrl')
                    ALTER TABLE [Users] DROP COLUMN [ProfileImageUrl];
            ");
        }
    }
}
