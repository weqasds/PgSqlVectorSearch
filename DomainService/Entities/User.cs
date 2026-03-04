using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DomainService.Entities;

public class User : Entity<int>
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
}
