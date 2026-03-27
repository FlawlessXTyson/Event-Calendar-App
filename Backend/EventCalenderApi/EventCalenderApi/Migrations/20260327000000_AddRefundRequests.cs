using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventCalenderApi.Migrations
{
    public partial class AddRefundRequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RefundRequests'
                )
                BEGIN
                    CREATE TABLE [RefundRequests] (
                        [RefundRequestId]    int          NOT NULL IDENTITY,
                        [UserId]             int          NOT NULL,
                        [EventId]            int          NOT NULL,
                        [PaymentId]          int          NOT NULL,
                        [RequestedAt]        datetime2    NOT NULL,
                        [Status]             int          NOT NULL DEFAULT 1,
                        [ApprovedPercentage] real         NULL,
                        [ReviewedByAdminId]  int          NULL,
                        [ReviewedAt]         datetime2    NULL,
                        CONSTRAINT [PK_RefundRequests] PRIMARY KEY ([RefundRequestId]),
                        CONSTRAINT [FK_RefundRequests_Users_UserId]
                            FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE,
                        CONSTRAINT [FK_RefundRequests_Events_EventId]
                            FOREIGN KEY ([EventId]) REFERENCES [Events] ([EventId]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_RefundRequests_Payments_PaymentId]
                            FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([PaymentId]) ON DELETE NO ACTION
                    );
                    CREATE INDEX [IX_RefundRequests_UserId]    ON [RefundRequests] ([UserId]);
                    CREATE INDEX [IX_RefundRequests_EventId]   ON [RefundRequests] ([EventId]);
                    CREATE INDEX [IX_RefundRequests_PaymentId] ON [RefundRequests] ([PaymentId]);
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RefundRequests')
                    DROP TABLE [RefundRequests];
            ");
        }
    }
}
