using System;
using System.ComponentModel.DataAnnotations;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.Enums;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models
{
    public class RoleChangeRequest
    {
        [Key]
        public int RequestId { get; set; }

        //foreign key → user who requested
        public int UserId { get; set; }

        //navigation property (nullable to avoid warning)
        public User? User { get; set; }

        //requested role
        public UserRole RequestedRole { get; set; }

        //request status
        public RequestStatus Status { get; set; } = RequestStatus.PENDING;

        //when request was created
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        //admin who reviewed
        public int? ReviewedByAdminId { get; set; }
    }
}