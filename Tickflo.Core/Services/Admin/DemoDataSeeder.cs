namespace Tickflo.Core.Services.Admin;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public interface IDemoDataSeeder
{
    public Task<bool> DemoWorkspaceExistsAsync();
    public Task SeedDemoDataAsync();
    public Task ResetDemoDataAsync();
}

public class DemoDataSeeder(TickfloDbContext dbContext) : IDemoDataSeeder
{
    private static readonly string[] DemoWorkspaceSlugs = ["tickflo-demo", "techstart", "global-services"];
    private static readonly string[] DemoUserEmails =
    [
        "admin@demo.com", "sarah@demo.com", "mike@demo.com",
        "lisa@demo.com", "tom@demo.com", "emma@demo.com"
    ];

    private readonly TickfloDbContext dbContext = dbContext;

    public async Task<bool> DemoWorkspaceExistsAsync() =>
        await this.dbContext.Workspaces.AnyAsync(w => w.Slug == "tickflo-demo");

    public async Task ResetDemoDataAsync()
    {
        await this.DeleteDemoDataAsync();
        await this.SeedDemoDataAsync();
    }

    public async Task SeedDemoDataAsync()
    {
        var now = DateTime.UtcNow;

        // =============================================
        // Users
        // Demo users have NULL passwords and confirmed emails.
        // They must set a password on first login.
        // =============================================
        var adminUser = new User { Name = "John Admin", Email = "admin@demo.com", EmailConfirmed = true, SystemAdmin = false, CreatedAt = now };
        var sarahUser = new User { Name = "Sarah Manager", Email = "sarah@demo.com", EmailConfirmed = true, SystemAdmin = false, CreatedAt = now };
        var mikeUser = new User { Name = "Mike Technician", Email = "mike@demo.com", EmailConfirmed = true, SystemAdmin = false, CreatedAt = now };
        var lisaUser = new User { Name = "Lisa Support", Email = "lisa@demo.com", EmailConfirmed = true, SystemAdmin = false, CreatedAt = now };
        var tomUser = new User { Name = "Tom Developer", Email = "tom@demo.com", EmailConfirmed = true, SystemAdmin = false, CreatedAt = now };
        var emmaUser = new User { Name = "Emma Sales", Email = "emma@demo.com", EmailConfirmed = true, SystemAdmin = false, CreatedAt = now };

        this.dbContext.Users.AddRange(adminUser, sarahUser, mikeUser, lisaUser, tomUser, emmaUser);
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Workspaces
        // =============================================
        var tickfloDemoWorkspace = new Workspace { Name = "Tickflo Demo", Slug = "tickflo-demo", CreatedBy = adminUser.Id, CreatedAt = now };
        var techstartWorkspace = new Workspace { Name = "TechStart Inc", Slug = "techstart", CreatedBy = adminUser.Id, CreatedAt = now };
        var globalServicesWorkspace = new Workspace { Name = "Global Services", Slug = "global-services", CreatedBy = adminUser.Id, CreatedAt = now };

        this.dbContext.Workspaces.AddRange(tickfloDemoWorkspace, techstartWorkspace, globalServicesWorkspace);
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // User Workspaces
        // =============================================
        this.dbContext.UserWorkspaces.AddRange(
            new UserWorkspace { UserId = adminUser.Id, WorkspaceId = tickfloDemoWorkspace.Id, Accepted = true, CreatedBy = adminUser.Id, CreatedAt = now },
            new UserWorkspace { UserId = sarahUser.Id, WorkspaceId = tickfloDemoWorkspace.Id, Accepted = true, CreatedBy = adminUser.Id, CreatedAt = now },
            new UserWorkspace { UserId = mikeUser.Id, WorkspaceId = tickfloDemoWorkspace.Id, Accepted = true, CreatedBy = adminUser.Id, CreatedAt = now },
            new UserWorkspace { UserId = lisaUser.Id, WorkspaceId = tickfloDemoWorkspace.Id, Accepted = true, CreatedBy = adminUser.Id, CreatedAt = now },
            new UserWorkspace { UserId = adminUser.Id, WorkspaceId = techstartWorkspace.Id, Accepted = true, CreatedBy = adminUser.Id, CreatedAt = now },
            new UserWorkspace { UserId = tomUser.Id, WorkspaceId = techstartWorkspace.Id, Accepted = true, CreatedBy = adminUser.Id, CreatedAt = now },
            new UserWorkspace { UserId = emmaUser.Id, WorkspaceId = techstartWorkspace.Id, Accepted = true, CreatedBy = adminUser.Id, CreatedAt = now },
            new UserWorkspace { UserId = adminUser.Id, WorkspaceId = globalServicesWorkspace.Id, Accepted = true, CreatedBy = adminUser.Id, CreatedAt = now },
            new UserWorkspace { UserId = sarahUser.Id, WorkspaceId = globalServicesWorkspace.Id, Accepted = true, CreatedBy = adminUser.Id, CreatedAt = now }
        );
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Permissions (upsert — skip if already exists)
        // =============================================
        var permissionDefs = new (string Resource, string Action)[]
        {
            ("tickets", "create"), ("tickets", "read"), ("tickets", "update"), ("tickets", "delete"), ("tickets", "assign"),
            ("contacts", "create"), ("contacts", "read"), ("contacts", "update"), ("contacts", "delete"),
            ("locations", "create"), ("locations", "read"), ("locations", "update"), ("locations", "delete"),
            ("inventory", "create"), ("inventory", "read"), ("inventory", "update"), ("inventory", "delete"),
            ("teams", "create"), ("teams", "read"), ("teams", "update"), ("teams", "delete"),
            ("reports", "create"), ("reports", "read"), ("reports", "update"), ("reports", "delete"), ("reports", "execute"),
            ("roles", "create"), ("roles", "read"), ("roles", "update"), ("roles", "delete"), ("roles", "assign"),
            ("users", "invite"), ("users", "read"), ("users", "update"), ("users", "delete"),
            ("workspace", "manage"),
        };

        foreach (var (resource, action) in permissionDefs)
        {
            var existing = await this.dbContext.Permissions
                .FirstOrDefaultAsync(p => p.Resource == resource && p.Action == action);

            if (existing == null)
            {
                this.dbContext.Permissions.Add(new Permission { Resource = resource, Action = action });
            }
        }

        await this.dbContext.SaveChangesAsync();

        var allPermissions = await this.dbContext.Permissions.ToListAsync();

        // =============================================
        // Roles
        // =============================================
        var demoAdminRole = new Role { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Admin", Admin = true, CreatedBy = adminUser.Id, CreatedAt = now };
        var demoManagerRole = new Role { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Manager", Admin = false, CreatedBy = adminUser.Id, CreatedAt = now };
        var demoTechnicianRole = new Role { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Technician", Admin = false, CreatedBy = adminUser.Id, CreatedAt = now };
        var demoSupportRole = new Role { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Support Agent", Admin = false, CreatedBy = adminUser.Id, CreatedAt = now };
        var techstartAdminRole = new Role { WorkspaceId = techstartWorkspace.Id, Name = "Admin", Admin = true, CreatedBy = adminUser.Id, CreatedAt = now };
        var techstartDeveloperRole = new Role { WorkspaceId = techstartWorkspace.Id, Name = "Developer", Admin = false, CreatedBy = adminUser.Id, CreatedAt = now };
        var techstartSalesRole = new Role { WorkspaceId = techstartWorkspace.Id, Name = "Sales Rep", Admin = false, CreatedBy = adminUser.Id, CreatedAt = now };
        var globalAdminRole = new Role { WorkspaceId = globalServicesWorkspace.Id, Name = "Admin", Admin = true, CreatedBy = adminUser.Id, CreatedAt = now };
        var globalOperatorRole = new Role { WorkspaceId = globalServicesWorkspace.Id, Name = "Operator", Admin = false, CreatedBy = adminUser.Id, CreatedAt = now };

        this.dbContext.Roles.AddRange(
            demoAdminRole, demoManagerRole, demoTechnicianRole, demoSupportRole,
            techstartAdminRole, techstartDeveloperRole, techstartSalesRole,
            globalAdminRole, globalOperatorRole
        );
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Role Permissions
        // =============================================
        var allResources = new[] { "tickets", "contacts", "locations", "inventory", "teams", "reports", "roles", "users", "workspace" };
        var managerResources = new[] { "tickets", "contacts", "locations", "inventory", "teams", "reports", "users" };
        var technicianResources = new[] { "tickets", "contacts", "inventory" };
        var technicianActions = new[] { "create", "read", "update" };
        var supportResources = new[] { "tickets", "contacts" };
        var supportActions = new[] { "create", "read", "update" };
        var developerResources = new[] { "tickets", "contacts", "inventory" };
        var salesResources = new[] { "contacts" };
        var operatorResources = new[] { "tickets", "locations", "inventory" };

        var rolePermissions = new List<RolePermission>();

        // Demo Admin — all permissions
        foreach (var perm in allPermissions)
        {
            rolePermissions.Add(new RolePermission { RoleId = demoAdminRole.Id, PermissionId = perm.Id, CreatedBy = adminUser.Id, CreatedAt = now });
        }

        // Demo Manager — selected resources (all actions)
        foreach (var perm in allPermissions.Where(p => managerResources.Contains(p.Resource)))
        {
            rolePermissions.Add(new RolePermission { RoleId = demoManagerRole.Id, PermissionId = perm.Id, CreatedBy = adminUser.Id, CreatedAt = now });
        }

        // Demo Technician — specific resources and actions
        foreach (var perm in allPermissions.Where(p => technicianResources.Contains(p.Resource) && technicianActions.Contains(p.Action)))
        {
            rolePermissions.Add(new RolePermission { RoleId = demoTechnicianRole.Id, PermissionId = perm.Id, CreatedBy = adminUser.Id, CreatedAt = now });
        }

        // Demo Support Agent — specific resources and actions
        foreach (var perm in allPermissions.Where(p => supportResources.Contains(p.Resource) && supportActions.Contains(p.Action)))
        {
            rolePermissions.Add(new RolePermission { RoleId = demoSupportRole.Id, PermissionId = perm.Id, CreatedBy = adminUser.Id, CreatedAt = now });
        }

        // TechStart Admin — all permissions
        foreach (var perm in allPermissions)
        {
            rolePermissions.Add(new RolePermission { RoleId = techstartAdminRole.Id, PermissionId = perm.Id, CreatedBy = adminUser.Id, CreatedAt = now });
        }

        // TechStart Developer — selected resources (all actions)
        foreach (var perm in allPermissions.Where(p => developerResources.Contains(p.Resource)))
        {
            rolePermissions.Add(new RolePermission { RoleId = techstartDeveloperRole.Id, PermissionId = perm.Id, CreatedBy = adminUser.Id, CreatedAt = now });
        }

        // TechStart Sales Rep — contacts (all) + tickets read
        foreach (var perm in allPermissions.Where(p => salesResources.Contains(p.Resource) || (p.Resource == "tickets" && p.Action == "read")))
        {
            rolePermissions.Add(new RolePermission { RoleId = techstartSalesRole.Id, PermissionId = perm.Id, CreatedBy = adminUser.Id, CreatedAt = now });
        }

        // Global Services Admin — all permissions
        foreach (var perm in allPermissions)
        {
            rolePermissions.Add(new RolePermission { RoleId = globalAdminRole.Id, PermissionId = perm.Id, CreatedBy = adminUser.Id, CreatedAt = now });
        }

        // Global Services Operator — specific resources (all actions)
        foreach (var perm in allPermissions.Where(p => operatorResources.Contains(p.Resource)))
        {
            rolePermissions.Add(new RolePermission { RoleId = globalOperatorRole.Id, PermissionId = perm.Id, CreatedBy = adminUser.Id, CreatedAt = now });
        }

        this.dbContext.RolePermissions.AddRange(rolePermissions);
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // User Workspace Roles
        // =============================================
        this.dbContext.UserWorkspaceRoles.AddRange(
            new UserWorkspaceRole { UserId = adminUser.Id, WorkspaceId = tickfloDemoWorkspace.Id, RoleId = demoAdminRole.Id, CreatedBy = adminUser.Id },
            new UserWorkspaceRole { UserId = sarahUser.Id, WorkspaceId = tickfloDemoWorkspace.Id, RoleId = demoManagerRole.Id, CreatedBy = adminUser.Id },
            new UserWorkspaceRole { UserId = mikeUser.Id, WorkspaceId = tickfloDemoWorkspace.Id, RoleId = demoTechnicianRole.Id, CreatedBy = adminUser.Id },
            new UserWorkspaceRole { UserId = lisaUser.Id, WorkspaceId = tickfloDemoWorkspace.Id, RoleId = demoSupportRole.Id, CreatedBy = adminUser.Id },
            new UserWorkspaceRole { UserId = adminUser.Id, WorkspaceId = techstartWorkspace.Id, RoleId = techstartAdminRole.Id, CreatedBy = adminUser.Id },
            new UserWorkspaceRole { UserId = tomUser.Id, WorkspaceId = techstartWorkspace.Id, RoleId = techstartDeveloperRole.Id, CreatedBy = adminUser.Id },
            new UserWorkspaceRole { UserId = emmaUser.Id, WorkspaceId = techstartWorkspace.Id, RoleId = techstartSalesRole.Id, CreatedBy = adminUser.Id },
            new UserWorkspaceRole { UserId = adminUser.Id, WorkspaceId = globalServicesWorkspace.Id, RoleId = globalAdminRole.Id, CreatedBy = adminUser.Id },
            new UserWorkspaceRole { UserId = sarahUser.Id, WorkspaceId = globalServicesWorkspace.Id, RoleId = globalOperatorRole.Id, CreatedBy = adminUser.Id }
        );
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Locations
        // =============================================
        var demoHq = new Location { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Headquarters", Address = "123 Main Street, New York, NY 10001", Active = true, CreatedBy = adminUser.Id, CreatedAt = now };
        var demoWestCoast = new Location { WorkspaceId = tickfloDemoWorkspace.Id, Name = "West Coast Office", Address = "456 Tech Blvd, San Francisco, CA 94105", Active = true, CreatedBy = adminUser.Id, CreatedAt = now };
        var demoChicago = new Location { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Chicago Branch", Address = "789 Lake Shore Dr, Chicago, IL 60611", Active = true, CreatedBy = adminUser.Id, CreatedAt = now };
        var demoWarehouse = new Location { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Warehouse A", Address = "321 Industrial Pkwy, Newark, NJ 07102", Active = true, CreatedBy = adminUser.Id, CreatedAt = now };
        var techstartMain = new Location { WorkspaceId = techstartWorkspace.Id, Name = "Main Office", Address = "555 Startup Ave, Austin, TX 78701", Active = true, CreatedBy = adminUser.Id, CreatedAt = now };
        var techstartLab = new Location { WorkspaceId = techstartWorkspace.Id, Name = "R&D Lab", Address = "777 Innovation Dr, Seattle, WA 98101", Active = true, CreatedBy = adminUser.Id, CreatedAt = now };
        var globalHub = new Location { WorkspaceId = globalServicesWorkspace.Id, Name = "Regional Hub", Address = "999 Commerce St, Dallas, TX 75201", Active = true, CreatedBy = adminUser.Id, CreatedAt = now };
        var globalService = new Location { WorkspaceId = globalServicesWorkspace.Id, Name = "Service Center", Address = "111 Support Way, Phoenix, AZ 85001", Active = true, CreatedBy = adminUser.Id, CreatedAt = now };

        this.dbContext.Locations.AddRange(demoHq, demoWestCoast, demoChicago, demoWarehouse, techstartMain, techstartLab, globalHub, globalService);
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Contacts
        // =============================================
        var robertContact = new Contact { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Robert Johnson", Email = "robert.j@client1.com", Phone = "555-0101", Company = "Client Corp A", Title = "IT Director", Notes = "Key decision maker for IT purchases", Tags = "vip,enterprise", PreferredChannel = "email", Priority = "High", Status = "Active", AssignedUserId = sarahUser.Id, LastInteraction = now.AddDays(-2), CreatedAt = now.AddDays(-30) };
        var jenniferContact = new Contact { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Jennifer Williams", Email = "jennifer.w@client2.com", Phone = "555-0102", Company = "Client Corp B", Title = "Operations Manager", Notes = "Handles day-to-day operations", Tags = "enterprise,support", PreferredChannel = "phone", Priority = "Normal", Status = "Active", AssignedUserId = mikeUser.Id, LastInteraction = now.AddDays(-5), CreatedAt = now.AddDays(-60) };
        var michaelContact = new Contact { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Michael Brown", Email = "michael.b@client3.com", Phone = "555-0103", Company = "Small Business Inc", Title = "Owner", Notes = "Small business client", Tags = "smb,friendly", PreferredChannel = "email", Priority = "Normal", Status = "Active", AssignedUserId = lisaUser.Id, LastInteraction = now.AddDays(-1), CreatedAt = now.AddDays(-15) };
        var emilyContact = new Contact { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Emily Davis", Email = "emily.d@client4.com", Phone = "555-0104", Company = "Enterprise Solutions", Title = "CTO", Notes = "Technical contact for large projects", Tags = "vip,technical", PreferredChannel = "email", Priority = "High", Status = "Active", AssignedUserId = sarahUser.Id, LastInteraction = now.AddDays(-3), CreatedAt = now.AddDays(-45) };
        var davidContact = new Contact { WorkspaceId = tickfloDemoWorkspace.Id, Name = "David Martinez", Email = "david.m@client5.com", Phone = "555-0105", Company = "MidSize Corp", Title = "Facilities Manager", Notes = "Manages facility issues", Tags = "facility,regular", PreferredChannel = "phone", Priority = "Normal", Status = "Active", AssignedUserId = mikeUser.Id, LastInteraction = now.AddDays(-7), CreatedAt = now.AddDays(-90) };
        var jessicaContact = new Contact { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Jessica Taylor", Email = "jessica.t@client6.com", Phone = "555-0106", Company = "Startup X", Title = "CEO", Notes = "Fast-growing startup", Tags = "startup,urgent", PreferredChannel = "email", Priority = "High", Status = "Active", AssignedUserId = sarahUser.Id, LastInteraction = now, CreatedAt = now.AddDays(-10) };
        var chrisContact = new Contact { WorkspaceId = techstartWorkspace.Id, Name = "Christopher Anderson", Email = "chris.a@prospect1.com", Phone = "555-0201", Company = "Prospect Alpha", Title = "VP Technology", Notes = "Evaluating our platform", Tags = "prospect,interested", PreferredChannel = "email", Priority = "High", Status = "Active", AssignedUserId = tomUser.Id, LastInteraction = now.AddDays(-1), CreatedAt = now.AddDays(-5) };
        var amandaContact = new Contact { WorkspaceId = techstartWorkspace.Id, Name = "Amanda White", Email = "amanda.w@customer1.com", Phone = "555-0202", Company = "Customer Beta", Title = "Product Manager", Notes = "Existing customer, very happy", Tags = "customer,advocate", PreferredChannel = "email", Priority = "Normal", Status = "Active", AssignedUserId = emmaUser.Id, LastInteraction = now.AddDays(-4), CreatedAt = now.AddDays(-120) };
        var jamesContact = new Contact { WorkspaceId = techstartWorkspace.Id, Name = "James Thomas", Email = "james.t@partner1.com", Phone = "555-0203", Company = "Partner Gamma", Title = "Business Development", Notes = "Strategic partner", Tags = "partner,strategic", PreferredChannel = "phone", Priority = "High", Status = "Active", AssignedUserId = tomUser.Id, LastInteraction = now.AddDays(-2), CreatedAt = now.AddDays(-20) };
        var lindaContact = new Contact { WorkspaceId = globalServicesWorkspace.Id, Name = "Linda Harris", Email = "linda.h@globclient1.com", Phone = "555-0301", Company = "Global Client 1", Title = "Regional Manager", Notes = "Multi-location client", Tags = "global,enterprise", PreferredChannel = "email", Priority = "High", Status = "Active", AssignedUserId = sarahUser.Id, LastInteraction = now.AddDays(-1), CreatedAt = now.AddDays(-40) };
        var danielContact = new Contact { WorkspaceId = globalServicesWorkspace.Id, Name = "Daniel Clark", Email = "daniel.c@globclient2.com", Phone = "555-0302", Company = "Global Client 2", Title = "Operations Director", Notes = "Complex service needs", Tags = "global,complex", PreferredChannel = "phone", Priority = "Normal", Status = "Active", AssignedUserId = sarahUser.Id, LastInteraction = now.AddDays(-6), CreatedAt = now.AddDays(-75) };

        this.dbContext.Contacts.AddRange(robertContact, jenniferContact, michaelContact, emilyContact, davidContact, jessicaContact, chrisContact, amandaContact, jamesContact, lindaContact, danielContact);
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Contact Locations
        // =============================================
        this.dbContext.ContactLocations.AddRange(
            new ContactLocation { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = robertContact.Id, LocationId = demoHq.Id },
            new ContactLocation { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = jenniferContact.Id, LocationId = demoWestCoast.Id },
            new ContactLocation { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = michaelContact.Id, LocationId = demoChicago.Id },
            new ContactLocation { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = emilyContact.Id, LocationId = demoHq.Id },
            new ContactLocation { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = emilyContact.Id, LocationId = demoWestCoast.Id },
            new ContactLocation { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = davidContact.Id, LocationId = demoChicago.Id },
            new ContactLocation { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = jessicaContact.Id, LocationId = demoHq.Id },
            new ContactLocation { WorkspaceId = techstartWorkspace.Id, ContactId = chrisContact.Id, LocationId = techstartMain.Id },
            new ContactLocation { WorkspaceId = techstartWorkspace.Id, ContactId = amandaContact.Id, LocationId = techstartMain.Id },
            new ContactLocation { WorkspaceId = techstartWorkspace.Id, ContactId = jamesContact.Id, LocationId = techstartLab.Id },
            new ContactLocation { WorkspaceId = globalServicesWorkspace.Id, ContactId = lindaContact.Id, LocationId = globalHub.Id },
            new ContactLocation { WorkspaceId = globalServicesWorkspace.Id, ContactId = lindaContact.Id, LocationId = globalService.Id },
            new ContactLocation { WorkspaceId = globalServicesWorkspace.Id, ContactId = danielContact.Id, LocationId = globalService.Id }
        );
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Teams
        // =============================================
        var fieldServicesTeam = new Team { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Field Services", Description = "On-site technical support team", CreatedBy = adminUser.Id, CreatedAt = now };
        var helpDeskTeam = new Team { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Help Desk", Description = "First line of support", CreatedBy = adminUser.Id, CreatedAt = now };
        var infrastructureTeam = new Team { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Infrastructure", Description = "IT infrastructure maintenance", CreatedBy = adminUser.Id, CreatedAt = now };
        var engineeringTeam = new Team { WorkspaceId = techstartWorkspace.Id, Name = "Engineering", Description = "Product development team", CreatedBy = adminUser.Id, CreatedAt = now };
        var customerSuccessTeam = new Team { WorkspaceId = techstartWorkspace.Id, Name = "Customer Success", Description = "Customer support and onboarding", CreatedBy = adminUser.Id, CreatedAt = now };
        var operationsTeam = new Team { WorkspaceId = globalServicesWorkspace.Id, Name = "Operations", Description = "Daily operations team", CreatedBy = adminUser.Id, CreatedAt = now };

        this.dbContext.Teams.AddRange(fieldServicesTeam, helpDeskTeam, infrastructureTeam, engineeringTeam, customerSuccessTeam, operationsTeam);
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Team Members
        // =============================================
        this.dbContext.TeamMembers.AddRange(
            new TeamMember { TeamId = fieldServicesTeam.Id, UserId = mikeUser.Id },
            new TeamMember { TeamId = helpDeskTeam.Id, UserId = lisaUser.Id },
            new TeamMember { TeamId = infrastructureTeam.Id, UserId = mikeUser.Id },
            new TeamMember { TeamId = engineeringTeam.Id, UserId = tomUser.Id },
            new TeamMember { TeamId = customerSuccessTeam.Id, UserId = emmaUser.Id },
            new TeamMember { TeamId = operationsTeam.Id, UserId = sarahUser.Id }
        );
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Inventory
        // =============================================
        var laptop001 = new Inventory { WorkspaceId = tickfloDemoWorkspace.Id, Sku = "LAPTOP-001", Name = "Dell Latitude 7420", Description = "i7-1185G7, 16GB RAM, 512GB SSD - Serial: DL7420-XYZ123", Quantity = 1, Category = "Laptops", Status = "active", LocationId = demoHq.Id, Cost = 1299.99m, Price = 1499.99m };
        var laptop002 = new Inventory { WorkspaceId = tickfloDemoWorkspace.Id, Sku = "LAPTOP-002", Name = "Dell Latitude 7420", Description = "i7-1185G7, 16GB RAM, 512GB SSD - Serial: DL7420-XYZ124", Quantity = 1, Category = "Laptops", Status = "active", LocationId = demoWestCoast.Id, Cost = 1299.99m, Price = 1499.99m };
        var desktop001 = new Inventory { WorkspaceId = tickfloDemoWorkspace.Id, Sku = "DESKTOP-001", Name = "HP EliteDesk 800 G6", Description = "i7-10700, 32GB RAM, 1TB SSD - Serial: HP800-ABC456", Quantity = 1, Category = "Desktops", Status = "active", LocationId = demoHq.Id, Cost = 1099.99m, Price = 1299.99m };
        var monitor001 = new Inventory { WorkspaceId = tickfloDemoWorkspace.Id, Sku = "MONITOR-001", Name = "Dell UltraSharp U2720Q", Description = "27\" 4K IPS Monitor - Serial: DU27-MON789", Quantity = 1, Category = "Monitors", Status = "active", LocationId = demoHq.Id, Cost = 499.99m, Price = 599.99m };
        var monitor002 = new Inventory { WorkspaceId = tickfloDemoWorkspace.Id, Sku = "MONITOR-002", Name = "Dell UltraSharp U2720Q", Description = "27\" 4K IPS Monitor - Serial: DU27-MON790", Quantity = 1, Category = "Monitors", Status = "active", LocationId = demoWestCoast.Id, Cost = 499.99m, Price = 599.99m };
        var printer001 = new Inventory { WorkspaceId = tickfloDemoWorkspace.Id, Sku = "PRINTER-001", Name = "HP LaserJet Pro M404dn", Description = "Monochrome laser printer - Serial: HPLJ-PRT001", Quantity = 1, Category = "Printers", Status = "active", LocationId = demoHq.Id, Cost = 299.99m, Price = 399.99m };
        var router001 = new Inventory { WorkspaceId = tickfloDemoWorkspace.Id, Sku = "ROUTER-001", Name = "Cisco Catalyst 9300", Description = "48-port managed switch - Serial: CC9300-NET001", Quantity = 1, Category = "Network", Status = "active", LocationId = demoHq.Id, Cost = 4199.99m, Price = 4999.99m };
        var server001 = new Inventory { WorkspaceId = tickfloDemoWorkspace.Id, Sku = "SERVER-001", Name = "Dell PowerEdge R740", Description = "Dual Xeon, 128GB RAM, RAID storage - Serial: DPE-R740-SRV01", Quantity = 1, Category = "Servers", Status = "active", LocationId = demoWarehouse.Id, Cost = 7999.99m, Price = 8999.99m };
        var phone001 = new Inventory { WorkspaceId = tickfloDemoWorkspace.Id, Sku = "PHONE-001", Name = "iPhone 14 Pro", Description = "256GB, Space Black - Serial: IP14P-PH001", Quantity = 1, Category = "Mobile Devices", Status = "active", LocationId = demoHq.Id, Cost = 899.99m, Price = 1099.99m };
        var tablet001 = new Inventory { WorkspaceId = tickfloDemoWorkspace.Id, Sku = "TABLET-001", Name = "iPad Pro 12.9\"", Description = "512GB, Wi-Fi + Cellular - Serial: IPP129-TAB01", Quantity = 1, Category = "Mobile Devices", Status = "active", LocationId = demoHq.Id, Cost = 1099.99m, Price = 1299.99m };
        var devLaptop001 = new Inventory { WorkspaceId = techstartWorkspace.Id, Sku = "DEV-LAPTOP-001", Name = "MacBook Pro 16\"", Description = "M2 Max, 32GB RAM, 1TB SSD - Serial: MBP16-DEV01", Quantity = 1, Category = "Laptops", Status = "active", LocationId = techstartMain.Id, Cost = 3099.99m, Price = 3499.99m };
        var devLaptop002 = new Inventory { WorkspaceId = techstartWorkspace.Id, Sku = "DEV-LAPTOP-002", Name = "MacBook Pro 16\"", Description = "M2 Max, 32GB RAM, 1TB SSD - Serial: MBP16-DEV02", Quantity = 1, Category = "Laptops", Status = "active", LocationId = techstartMain.Id, Cost = 3099.99m, Price = 3499.99m };
        var devMonitor001 = new Inventory { WorkspaceId = techstartWorkspace.Id, Sku = "DEV-MONITOR-001", Name = "LG UltraWide 38\"", Description = "38\" Curved UltraWide - Serial: LG38-MON01", Quantity = 1, Category = "Monitors", Status = "active", LocationId = techstartMain.Id, Cost = 1099.99m, Price = 1299.99m };
        var gsLaptop001 = new Inventory { WorkspaceId = globalServicesWorkspace.Id, Sku = "GS-LAPTOP-001", Name = "Lenovo ThinkPad X1 Carbon", Description = "i7-1260P, 16GB RAM, 512GB SSD - Serial: LTX1C-GS01", Quantity = 1, Category = "Laptops", Status = "active", LocationId = globalHub.Id, Cost = 1699.99m, Price = 1899.99m };

        this.dbContext.Inventory.AddRange(laptop001, laptop002, desktop001, monitor001, monitor002, printer001, router001, server001, phone001, tablet001, devLaptop001, devLaptop002, devMonitor001, gsLaptop001);
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Ticket Statuses
        // =============================================
        var demoStatusNew = new TicketStatus { WorkspaceId = tickfloDemoWorkspace.Id, Name = "New", Color = "info", SortOrder = 0 };
        var demoStatusInProgress = new TicketStatus { WorkspaceId = tickfloDemoWorkspace.Id, Name = "In Progress", Color = "warning", SortOrder = 1 };
        var demoStatusWaiting = new TicketStatus { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Waiting", Color = "neutral", SortOrder = 2 };
        var demoStatusResolved = new TicketStatus { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Resolved", Color = "success", SortOrder = 3 };
        var demoStatusClosed = new TicketStatus { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Closed", Color = "neutral", SortOrder = 4 };
        var techStatusOpen = new TicketStatus { WorkspaceId = techstartWorkspace.Id, Name = "Open", Color = "info", SortOrder = 0 };
        var techStatusWorking = new TicketStatus { WorkspaceId = techstartWorkspace.Id, Name = "Working", Color = "warning", SortOrder = 1 };
        var techStatusDone = new TicketStatus { WorkspaceId = techstartWorkspace.Id, Name = "Done", Color = "success", SortOrder = 2 };
        var globalStatusSubmitted = new TicketStatus { WorkspaceId = globalServicesWorkspace.Id, Name = "Submitted", Color = "info", SortOrder = 0 };
        var globalStatusAssigned = new TicketStatus { WorkspaceId = globalServicesWorkspace.Id, Name = "Assigned", Color = "warning", SortOrder = 1 };
        var globalStatusCompleted = new TicketStatus { WorkspaceId = globalServicesWorkspace.Id, Name = "Completed", Color = "success", SortOrder = 2 };

        this.dbContext.TicketStatuses.AddRange(demoStatusNew, demoStatusInProgress, demoStatusWaiting, demoStatusResolved, demoStatusClosed, techStatusOpen, techStatusWorking, techStatusDone, globalStatusSubmitted, globalStatusAssigned, globalStatusCompleted);
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Ticket Priorities
        // =============================================
        var demoPriorityLow = new TicketPriority { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Low", Color = "success", SortOrder = 0 };
        var demoPriorityNormal = new TicketPriority { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Normal", Color = "info", SortOrder = 1 };
        var demoPriorityHigh = new TicketPriority { WorkspaceId = tickfloDemoWorkspace.Id, Name = "High", Color = "warning", SortOrder = 2 };
        var demoPriorityUrgent = new TicketPriority { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Urgent", Color = "error", SortOrder = 3 };
        var techPriorityP3 = new TicketPriority { WorkspaceId = techstartWorkspace.Id, Name = "P3", Color = "neutral", SortOrder = 0 };
        var techPriorityP2 = new TicketPriority { WorkspaceId = techstartWorkspace.Id, Name = "P2", Color = "warning", SortOrder = 1 };
        var techPriorityP1 = new TicketPriority { WorkspaceId = techstartWorkspace.Id, Name = "P1", Color = "error", SortOrder = 2 };
        var globalPriorityLow = new TicketPriority { WorkspaceId = globalServicesWorkspace.Id, Name = "Low", Color = "success", SortOrder = 0 };
        var globalPriorityMedium = new TicketPriority { WorkspaceId = globalServicesWorkspace.Id, Name = "Medium", Color = "warning", SortOrder = 1 };
        var globalPriorityHigh = new TicketPriority { WorkspaceId = globalServicesWorkspace.Id, Name = "High", Color = "error", SortOrder = 2 };

        this.dbContext.TicketPriorities.AddRange(demoPriorityLow, demoPriorityNormal, demoPriorityHigh, demoPriorityUrgent, techPriorityP3, techPriorityP2, techPriorityP1, globalPriorityLow, globalPriorityMedium, globalPriorityHigh);
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Ticket Types
        // =============================================
        var demoTypeIncident = new TicketType { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Incident", Color = "error", SortOrder = 0 };
        var demoTypeServiceRequest = new TicketType { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Service Request", Color = "info", SortOrder = 1 };
        var demoTypeChangeRequest = new TicketType { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Change Request", Color = "warning", SortOrder = 2 };
        var demoTypeProblem = new TicketType { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Problem", Color = "error", SortOrder = 3 };
        var demoTypeQuestion = new TicketType { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Question", Color = "info", SortOrder = 4 };
        var techTypeBug = new TicketType { WorkspaceId = techstartWorkspace.Id, Name = "Bug", Color = "error", SortOrder = 0 };
        var techTypeFeatureRequest = new TicketType { WorkspaceId = techstartWorkspace.Id, Name = "Feature Request", Color = "success", SortOrder = 1 };
        var techTypeSupport = new TicketType { WorkspaceId = techstartWorkspace.Id, Name = "Support", Color = "info", SortOrder = 2 };
        var globalTypeMaintenance = new TicketType { WorkspaceId = globalServicesWorkspace.Id, Name = "Maintenance", Color = "warning", SortOrder = 0 };
        var globalTypeRepair = new TicketType { WorkspaceId = globalServicesWorkspace.Id, Name = "Repair", Color = "error", SortOrder = 1 };
        var globalTypeInstallation = new TicketType { WorkspaceId = globalServicesWorkspace.Id, Name = "Installation", Color = "info", SortOrder = 2 };

        this.dbContext.TicketTypes.AddRange(demoTypeIncident, demoTypeServiceRequest, demoTypeChangeRequest, demoTypeProblem, demoTypeQuestion, techTypeBug, techTypeFeatureRequest, techTypeSupport, globalTypeMaintenance, globalTypeRepair, globalTypeInstallation);
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Tickets
        // =============================================
        var ticket01 = new Ticket { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = robertContact.Id, LocationId = demoHq.Id, Subject = "Email not syncing on mobile device", Description = "Robert reports that his iPhone is not syncing emails since this morning. He can receive but not send.", TicketTypeId = demoTypeIncident.Id, PriorityId = demoPriorityHigh.Id, StatusId = demoStatusInProgress.Id, AssignedUserId = mikeUser.Id, AssignedTeamId = fieldServicesTeam.Id, CreatedAt = now.AddHours(-2), UpdatedAt = now.AddHours(-1) };
        var ticket02 = new Ticket { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = jenniferContact.Id, LocationId = demoWestCoast.Id, Subject = "Request new laptop for new hire", Description = "Jennifer needs a new laptop configured for their new IT specialist starting next Monday.", TicketTypeId = demoTypeServiceRequest.Id, PriorityId = demoPriorityNormal.Id, StatusId = demoStatusNew.Id, AssignedUserId = sarahUser.Id, CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) };
        var ticket03 = new Ticket { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = michaelContact.Id, LocationId = demoChicago.Id, Subject = "Printer paper jam recurring issue", Description = "Michael says the office printer keeps jamming. Might need maintenance or replacement.", TicketTypeId = demoTypeProblem.Id, PriorityId = demoPriorityNormal.Id, StatusId = demoStatusWaiting.Id, AssignedUserId = mikeUser.Id, AssignedTeamId = fieldServicesTeam.Id, CreatedAt = now.AddDays(-3), UpdatedAt = now.AddDays(-1) };
        var ticket04 = new Ticket { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = emilyContact.Id, LocationId = demoHq.Id, Subject = "VPN connection dropping frequently", Description = "Emily experiences VPN disconnections every 10-15 minutes when working remotely.", TicketTypeId = demoTypeIncident.Id, PriorityId = demoPriorityHigh.Id, StatusId = demoStatusInProgress.Id, AssignedUserId = mikeUser.Id, AssignedTeamId = infrastructureTeam.Id, CreatedAt = now.AddHours(-4), UpdatedAt = now.AddHours(-2) };
        var ticket05 = new Ticket { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = davidContact.Id, LocationId = demoChicago.Id, Subject = "Install new access control system", Description = "David requests installation of card readers at the Chicago facility entrance.", TicketTypeId = demoTypeChangeRequest.Id, PriorityId = demoPriorityNormal.Id, StatusId = demoStatusNew.Id, CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-2) };
        var ticket06 = new Ticket { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = jessicaContact.Id, LocationId = demoHq.Id, Subject = "Software license inquiry", Description = "Jessica asks about available licenses for Adobe Creative Cloud for her design team.", TicketTypeId = demoTypeQuestion.Id, PriorityId = demoPriorityLow.Id, StatusId = demoStatusResolved.Id, AssignedUserId = lisaUser.Id, AssignedTeamId = helpDeskTeam.Id, CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-4) };
        var ticket07 = new Ticket { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = robertContact.Id, LocationId = demoHq.Id, Subject = "Cannot access shared drive", Description = "Robert cannot access the Finance shared drive. Getting \"Access Denied\" error.", TicketTypeId = demoTypeIncident.Id, PriorityId = demoPriorityHigh.Id, StatusId = demoStatusResolved.Id, AssignedUserId = mikeUser.Id, AssignedTeamId = fieldServicesTeam.Id, CreatedAt = now.AddDays(-7), UpdatedAt = now.AddDays(-6) };
        var ticket08 = new Ticket { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = jenniferContact.Id, LocationId = demoWestCoast.Id, Subject = "Monitor flickering issue", Description = "Jennifer's monitor flickers intermittently. Might be cable or monitor issue.", TicketTypeId = demoTypeIncident.Id, PriorityId = demoPriorityNormal.Id, StatusId = demoStatusClosed.Id, AssignedUserId = mikeUser.Id, AssignedTeamId = fieldServicesTeam.Id, CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-9) };
        var ticket09 = new Ticket { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = emilyContact.Id, LocationId = demoHq.Id, Subject = "Upgrade Office 365 subscription", Description = "Emily's team needs upgraded Office 365 licenses with advanced features.", TicketTypeId = demoTypeServiceRequest.Id, PriorityId = demoPriorityNormal.Id, StatusId = demoStatusClosed.Id, AssignedUserId = sarahUser.Id, CreatedAt = now.AddDays(-15), UpdatedAt = now.AddDays(-12) };
        var ticket10 = new Ticket { WorkspaceId = tickfloDemoWorkspace.Id, ContactId = michaelContact.Id, LocationId = demoChicago.Id, Subject = "WiFi slow in conference room", Description = "Michael reports very slow WiFi speeds in the main conference room during meetings.", TicketTypeId = demoTypeProblem.Id, PriorityId = demoPriorityNormal.Id, StatusId = demoStatusResolved.Id, AssignedUserId = mikeUser.Id, AssignedTeamId = infrastructureTeam.Id, CreatedAt = now.AddDays(-8), UpdatedAt = now.AddDays(-7) };
        var ticket11 = new Ticket { WorkspaceId = techstartWorkspace.Id, ContactId = chrisContact.Id, LocationId = techstartMain.Id, Subject = "Login page not loading", Description = "Christopher reports that the login page shows a blank screen on Chrome.", TicketTypeId = techTypeBug.Id, PriorityId = techPriorityP1.Id, StatusId = techStatusWorking.Id, AssignedUserId = tomUser.Id, AssignedTeamId = engineeringTeam.Id, CreatedAt = now.AddHours(-3), UpdatedAt = now.AddHours(-1) };
        var ticket12 = new Ticket { WorkspaceId = techstartWorkspace.Id, ContactId = amandaContact.Id, LocationId = techstartMain.Id, Subject = "Feature request: Dark mode", Description = "Amanda suggests adding a dark mode option for the dashboard interface.", TicketTypeId = techTypeFeatureRequest.Id, PriorityId = techPriorityP3.Id, StatusId = techStatusOpen.Id, CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-5) };
        var ticket13 = new Ticket { WorkspaceId = techstartWorkspace.Id, ContactId = jamesContact.Id, LocationId = techstartLab.Id, Subject = "API documentation outdated", Description = "James mentions that the API docs for v2.3 still reference old endpoints.", TicketTypeId = techTypeSupport.Id, PriorityId = techPriorityP2.Id, StatusId = techStatusWorking.Id, AssignedUserId = tomUser.Id, AssignedTeamId = engineeringTeam.Id, CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-1) };
        var ticket14 = new Ticket { WorkspaceId = techstartWorkspace.Id, ContactId = chrisContact.Id, LocationId = techstartMain.Id, Subject = "Export function timeout", Description = "Christopher gets timeout errors when exporting large reports (>10k rows).", TicketTypeId = techTypeBug.Id, PriorityId = techPriorityP2.Id, StatusId = techStatusOpen.Id, AssignedTeamId = engineeringTeam.Id, CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) };
        var ticket15 = new Ticket { WorkspaceId = techstartWorkspace.Id, ContactId = amandaContact.Id, LocationId = techstartMain.Id, Subject = "SSO integration with Azure AD", Description = "Amanda wants to enable SSO for her team using Azure Active Directory.", TicketTypeId = techTypeFeatureRequest.Id, PriorityId = techPriorityP2.Id, StatusId = techStatusDone.Id, AssignedUserId = tomUser.Id, AssignedTeamId = engineeringTeam.Id, CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-15) };
        var ticket16 = new Ticket { WorkspaceId = globalServicesWorkspace.Id, ContactId = lindaContact.Id, LocationId = globalHub.Id, Subject = "HVAC system making loud noise", Description = "Linda reports unusual sounds from the air conditioning unit in the server room.", TicketTypeId = globalTypeMaintenance.Id, PriorityId = globalPriorityHigh.Id, StatusId = globalStatusAssigned.Id, AssignedUserId = sarahUser.Id, AssignedTeamId = operationsTeam.Id, CreatedAt = now.AddHours(-6), UpdatedAt = now.AddHours(-4) };
        var ticket17 = new Ticket { WorkspaceId = globalServicesWorkspace.Id, ContactId = danielContact.Id, LocationId = globalService.Id, Subject = "Replace broken door lock", Description = "Daniel says the main entrance lock is stuck and needs replacement.", TicketTypeId = globalTypeRepair.Id, PriorityId = globalPriorityMedium.Id, StatusId = globalStatusSubmitted.Id, CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) };
        var ticket18 = new Ticket { WorkspaceId = globalServicesWorkspace.Id, ContactId = lindaContact.Id, LocationId = globalHub.Id, Subject = "Fire alarm system annual test", Description = "Linda schedules annual fire alarm testing for next week.", TicketTypeId = globalTypeMaintenance.Id, PriorityId = globalPriorityMedium.Id, StatusId = globalStatusSubmitted.Id, AssignedUserId = sarahUser.Id, AssignedTeamId = operationsTeam.Id, CreatedAt = now.AddDays(-3), UpdatedAt = now.AddDays(-3) };
        var ticket19 = new Ticket { WorkspaceId = globalServicesWorkspace.Id, ContactId = danielContact.Id, LocationId = globalService.Id, Subject = "Emergency lighting installation", Description = "Daniel requests installation of emergency exit lighting in the new warehouse section.", TicketTypeId = globalTypeInstallation.Id, PriorityId = globalPriorityHigh.Id, StatusId = globalStatusAssigned.Id, AssignedUserId = sarahUser.Id, AssignedTeamId = operationsTeam.Id, CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-1) };
        var ticket20 = new Ticket { WorkspaceId = globalServicesWorkspace.Id, ContactId = lindaContact.Id, LocationId = globalService.Id, Subject = "Security camera not recording", Description = "Linda notices that camera #3 in the parking lot is not recording footage.", TicketTypeId = globalTypeRepair.Id, PriorityId = globalPriorityHigh.Id, StatusId = globalStatusCompleted.Id, AssignedUserId = sarahUser.Id, AssignedTeamId = operationsTeam.Id, CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-4) };

        this.dbContext.Tickets.AddRange(ticket01, ticket02, ticket03, ticket04, ticket05, ticket06, ticket07, ticket08, ticket09, ticket10, ticket11, ticket12, ticket13, ticket14, ticket15, ticket16, ticket17, ticket18, ticket19, ticket20);
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Ticket Inventory
        // =============================================
        this.dbContext.TicketInventories.AddRange(
            new TicketInventory { TicketId = ticket01.Id, InventoryId = phone001.Id },
            new TicketInventory { TicketId = ticket02.Id, InventoryId = tablet001.Id },
            new TicketInventory { TicketId = ticket03.Id, InventoryId = printer001.Id },
            new TicketInventory { TicketId = ticket04.Id, InventoryId = laptop001.Id },
            new TicketInventory { TicketId = ticket08.Id, InventoryId = monitor001.Id },
            new TicketInventory { TicketId = ticket10.Id, InventoryId = router001.Id }
        );
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Ticket History
        // =============================================
        this.dbContext.TicketHistory.AddRange(
            new TicketHistory { WorkspaceId = tickfloDemoWorkspace.Id, TicketId = ticket01.Id, CreatedByUserId = mikeUser.Id, Action = TicketHistoryAction.Created, CreatedAt = now.AddMinutes(-90) },
            new TicketHistory { WorkspaceId = tickfloDemoWorkspace.Id, TicketId = ticket01.Id, CreatedByUserId = mikeUser.Id, Action = TicketHistoryAction.Assigned, Note = "Assigned to Mike", CreatedAt = now.AddMinutes(-75) },
            new TicketHistory { WorkspaceId = tickfloDemoWorkspace.Id, TicketId = ticket01.Id, CreatedByUserId = mikeUser.Id, Action = TicketHistoryAction.FieldChanged, Field = TicketHistoryField.Status, OldValue = "New", NewValue = "In Progress", Note = "Starting investigation", CreatedAt = now.AddHours(-1) },
            new TicketHistory { WorkspaceId = tickfloDemoWorkspace.Id, TicketId = ticket04.Id, CreatedByUserId = mikeUser.Id, Action = TicketHistoryAction.TeamAssigned, Note = "Routed to infrastructure team", CreatedAt = now.AddMinutes(-150) },
            new TicketHistory { WorkspaceId = tickfloDemoWorkspace.Id, TicketId = ticket04.Id, CreatedByUserId = mikeUser.Id, Action = TicketHistoryAction.FieldChanged, Field = TicketHistoryField.Status, OldValue = "New", NewValue = "In Progress", Note = "Assigned to infrastructure team", CreatedAt = now.AddHours(-2) },
            new TicketHistory { WorkspaceId = tickfloDemoWorkspace.Id, TicketId = ticket06.Id, CreatedByUserId = lisaUser.Id, Action = TicketHistoryAction.FieldChanged, Field = TicketHistoryField.Status, OldValue = "New", NewValue = "In Progress", Note = "Checking license availability", CreatedAt = now.AddDays(-5) },
            new TicketHistory { WorkspaceId = tickfloDemoWorkspace.Id, TicketId = ticket06.Id, CreatedByUserId = lisaUser.Id, Action = TicketHistoryAction.Resolved, Note = "Marked as resolved", CreatedAt = now.AddDays(-4) },
            new TicketHistory { WorkspaceId = tickfloDemoWorkspace.Id, TicketId = ticket07.Id, CreatedByUserId = mikeUser.Id, Action = TicketHistoryAction.FieldChanged, Field = TicketHistoryField.Status, OldValue = "New", NewValue = "In Progress", Note = "Investigating permissions", CreatedAt = now.AddDays(-7) },
            new TicketHistory { WorkspaceId = tickfloDemoWorkspace.Id, TicketId = ticket07.Id, CreatedByUserId = mikeUser.Id, Action = TicketHistoryAction.Resolved, Note = "Access restored", CreatedAt = now.AddDays(-6) },
            new TicketHistory { WorkspaceId = tickfloDemoWorkspace.Id, TicketId = ticket07.Id, CreatedByUserId = mikeUser.Id, Action = TicketHistoryAction.Closed, Note = "Closed after user confirmation", CreatedAt = now.AddDays(-5).AddHours(-12) },
            new TicketHistory { WorkspaceId = techstartWorkspace.Id, TicketId = ticket11.Id, CreatedByUserId = tomUser.Id, Action = TicketHistoryAction.FieldChanged, Field = TicketHistoryField.Status, OldValue = "Open", NewValue = "Working", Note = "Reproducing the issue", CreatedAt = now.AddHours(-2) },
            new TicketHistory { WorkspaceId = techstartWorkspace.Id, TicketId = ticket11.Id, CreatedByUserId = tomUser.Id, Action = TicketHistoryAction.FieldChanged, Field = TicketHistoryField.Priority, OldValue = "P2", NewValue = "P1", Note = "Escalating - affects multiple users", CreatedAt = now.AddHours(-1) },
            new TicketHistory { WorkspaceId = techstartWorkspace.Id, TicketId = ticket11.Id, CreatedByUserId = tomUser.Id, Action = TicketHistoryAction.ReassignmentNote, Note = "Reassigned from Lisa to Tom for frontend expertise", CreatedAt = now.AddMinutes(-45) },
            new TicketHistory { WorkspaceId = globalServicesWorkspace.Id, TicketId = ticket16.Id, CreatedByUserId = sarahUser.Id, Action = TicketHistoryAction.Created, CreatedAt = now.AddHours(-5) },
            new TicketHistory { WorkspaceId = globalServicesWorkspace.Id, TicketId = ticket16.Id, CreatedByUserId = sarahUser.Id, Action = TicketHistoryAction.FieldChanged, Field = TicketHistoryField.Status, OldValue = "Submitted", NewValue = "Assigned", Note = "Scheduled technician visit", CreatedAt = now.AddHours(-4) },
            new TicketHistory { WorkspaceId = globalServicesWorkspace.Id, TicketId = ticket16.Id, CreatedByUserId = sarahUser.Id, Action = TicketHistoryAction.Unassigned, Note = "Removing previous assignee before routing", CreatedAt = now.AddHours(-3).AddMinutes(-30) }
        );
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Ticket Comments
        // =============================================
        this.dbContext.TicketComments.AddRange(
            new TicketComment { WorkspaceId = tickfloDemoWorkspace.Id, TicketId = ticket01.Id, CreatedByUserId = mikeUser.Id, Content = "Checked email server logs. Issue appears to be with device configuration.", IsVisibleToClient = false, CreatedAt = now.AddMinutes(-45) },
            new TicketComment { WorkspaceId = tickfloDemoWorkspace.Id, TicketId = ticket04.Id, CreatedByUserId = mikeUser.Id, Content = "Updated VPN client to latest version. Monitoring for stability.", IsVisibleToClient = true, CreatedAt = now.AddHours(-1) },
            new TicketComment { WorkspaceId = tickfloDemoWorkspace.Id, TicketId = ticket06.Id, CreatedByUserId = lisaUser.Id, Content = "We have 5 available licenses. Sent details to Jessica.", IsVisibleToClient = true, CreatedAt = now.AddDays(-4).AddHours(-12) },
            new TicketComment { WorkspaceId = tickfloDemoWorkspace.Id, TicketId = ticket07.Id, CreatedByUserId = mikeUser.Id, Content = "Found the issue - AD group membership was missing. Added user to correct group.", IsVisibleToClient = false, CreatedAt = now.AddDays(-6).AddHours(-18) },
            new TicketComment { WorkspaceId = techstartWorkspace.Id, TicketId = ticket11.Id, CreatedByUserId = tomUser.Id, Content = "Issue traced to caching problem. Deploying fix now.", IsVisibleToClient = true, CreatedAt = now.AddMinutes(-30) },
            new TicketComment { WorkspaceId = globalServicesWorkspace.Id, TicketId = ticket16.Id, CreatedByUserId = sarahUser.Id, Content = "HVAC technician will arrive at 2 PM today.", IsVisibleToClient = true, CreatedAt = now.AddHours(-3) }
        );
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Reports
        // =============================================
        var reportOpenTicketsByPriority = new Report { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Open Tickets by Priority", Ready = true, LastRun = now.AddDays(-1) };
        var reportMonthlyResolutionTime = new Report { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Monthly Resolution Time", Ready = true, LastRun = now.AddDays(-2) };
        var reportTicketsByLocation = new Report { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Tickets by Location", Ready = true, LastRun = now.AddDays(-3) };
        var reportAssetWarrantyExpiry = new Report { WorkspaceId = tickfloDemoWorkspace.Id, Name = "Asset Warranty Expiry", Ready = true, LastRun = now.AddDays(-5) };
        var reportBugSummary = new Report { WorkspaceId = techstartWorkspace.Id, Name = "Bug Report Summary", Ready = true, LastRun = now.AddDays(-1) };
        var reportCustomerSatisfaction = new Report { WorkspaceId = techstartWorkspace.Id, Name = "Customer Satisfaction", Ready = false };
        var reportMaintenanceSchedule = new Report { WorkspaceId = globalServicesWorkspace.Id, Name = "Maintenance Schedule", Ready = true, LastRun = now.AddDays(-4) };

        this.dbContext.Reports.AddRange(reportOpenTicketsByPriority, reportMonthlyResolutionTime, reportTicketsByLocation, reportAssetWarrantyExpiry, reportBugSummary, reportCustomerSatisfaction, reportMaintenanceSchedule);
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Report Runs
        // =============================================
        this.dbContext.ReportRuns.AddRange(
            new ReportRun { WorkspaceId = tickfloDemoWorkspace.Id, ReportId = reportOpenTicketsByPriority.Id, Status = "Completed", StartedAt = now.AddDays(-1), FinishedAt = now.AddDays(-1) },
            new ReportRun { WorkspaceId = tickfloDemoWorkspace.Id, ReportId = reportOpenTicketsByPriority.Id, Status = "Completed", StartedAt = now.AddDays(-7), FinishedAt = now.AddDays(-7) },
            new ReportRun { WorkspaceId = tickfloDemoWorkspace.Id, ReportId = reportMonthlyResolutionTime.Id, Status = "Completed", StartedAt = now.AddDays(-2), FinishedAt = now.AddDays(-2) },
            new ReportRun { WorkspaceId = tickfloDemoWorkspace.Id, ReportId = reportTicketsByLocation.Id, Status = "Completed", StartedAt = now.AddDays(-3), FinishedAt = now.AddDays(-3) },
            new ReportRun { WorkspaceId = tickfloDemoWorkspace.Id, ReportId = reportAssetWarrantyExpiry.Id, Status = "Completed", StartedAt = now.AddDays(-5), FinishedAt = now.AddDays(-5) },
            new ReportRun { WorkspaceId = techstartWorkspace.Id, ReportId = reportBugSummary.Id, Status = "Completed", StartedAt = now.AddDays(-1), FinishedAt = now.AddDays(-1) },
            new ReportRun { WorkspaceId = globalServicesWorkspace.Id, ReportId = reportMaintenanceSchedule.Id, Status = "Completed", StartedAt = now.AddDays(-4), FinishedAt = now.AddDays(-4) }
        );
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // Notifications
        // =============================================
        var failedNotification = new Notification { WorkspaceId = techstartWorkspace.Id, UserId = tomUser.Id, Type = "password_reset", DeliveryMethod = "email", Priority = "urgent", Subject = "Password Reset Request", Body = "<p>You requested a password reset for your account.</p><p>Click the link below to reset your password:</p>", Status = "failed", CreatedAt = now.AddDays(-1), FailedAt = now.AddDays(-1), FailureReason = "SMTP connection timeout" };

        this.dbContext.Notifications.AddRange(
            new Notification { WorkspaceId = tickfloDemoWorkspace.Id, UserId = sarahUser.Id, Type = "workspace_invite", DeliveryMethod = "email", Priority = "high", Subject = "Welcome to Acme Corporation", Body = "<p>You have been invited to join <strong>Acme Corporation</strong> workspace.</p><p>Click here to accept the invitation and get started.</p>", Status = "sent", CreatedAt = now.AddDays(-3), CreatedBy = adminUser.Id },
            new Notification { WorkspaceId = tickfloDemoWorkspace.Id, UserId = mikeUser.Id, Type = "ticket_assigned", DeliveryMethod = "email", Priority = "normal", Subject = "Ticket #1001 assigned to you", Body = "<p>A new ticket has been assigned to you:</p><p><strong>Title:</strong> Email not syncing on mobile device</p><p><strong>Priority:</strong> High</p>", Status = "sent", CreatedAt = now.AddDays(-2), CreatedBy = sarahUser.Id },
            new Notification { WorkspaceId = techstartWorkspace.Id, UserId = tomUser.Id, Type = "ticket_comment", DeliveryMethod = "email", Priority = "normal", Subject = "New comment on Ticket #1005", Body = "<p>Lisa Johnson added a comment to your ticket:</p><blockquote>I have identified the root cause. Will update shortly.</blockquote>", Status = "sent", CreatedAt = now.AddDays(-1), CreatedBy = lisaUser.Id },
            new Notification { WorkspaceId = tickfloDemoWorkspace.Id, UserId = sarahUser.Id, Type = "ticket_status_changed", DeliveryMethod = "in_app", Priority = "normal", Subject = "Ticket #1001 status updated", Body = "Ticket status changed from Open to In Progress", Status = "sent", CreatedAt = now.AddHours(-2), CreatedBy = mikeUser.Id },
            new Notification { WorkspaceId = tickfloDemoWorkspace.Id, UserId = mikeUser.Id, Type = "ticket_assigned", DeliveryMethod = "in_app", Priority = "normal", Subject = "New ticket assigned", Body = "Ticket #1015 \"Network connectivity issues\" has been assigned to you", Status = "sent", CreatedAt = now.AddHours(-1), CreatedBy = sarahUser.Id },
            new Notification { WorkspaceId = tickfloDemoWorkspace.Id, UserId = lisaUser.Id, Type = "ticket_created_team", DeliveryMethod = "in_app", Priority = "normal", Subject = "New ticket for your team", Body = "A new ticket was created and routed to your team", Status = "sent", CreatedAt = now.AddMinutes(-90), CreatedBy = sarahUser.Id },
            new Notification { WorkspaceId = tickfloDemoWorkspace.Id, UserId = mikeUser.Id, Type = "ticket_unassigned", DeliveryMethod = "in_app", Priority = "low", Subject = "Ticket unassigned", Body = "Ticket #1012 was unassigned from you", Status = "sent", CreatedAt = now.AddMinutes(-45), CreatedBy = sarahUser.Id },
            new Notification { WorkspaceId = techstartWorkspace.Id, UserId = tomUser.Id, Type = "ticket_status_changed_team", DeliveryMethod = "in_app", Priority = "normal", Subject = "Team ticket status updated", Body = "A ticket assigned to your team changed status", Status = "sent", CreatedAt = now.AddMinutes(-20), CreatedBy = lisaUser.Id },
            new Notification { WorkspaceId = techstartWorkspace.Id, UserId = lisaUser.Id, Type = "report_completed", DeliveryMethod = "in_app", Priority = "low", Subject = "Weekly report completed", Body = "Your weekly ticket report has finished processing", Status = "sent", CreatedAt = now.AddMinutes(-30) },
            new Notification { WorkspaceId = globalServicesWorkspace.Id, UserId = emmaUser.Id, Type = "mention", DeliveryMethod = "in_app", Priority = "high", Subject = "You were mentioned in a comment", Body = "Tom Wilson mentioned you in ticket #1012", Status = "pending", CreatedAt = now.AddMinutes(-15), CreatedBy = tomUser.Id },
            new Notification { WorkspaceId = tickfloDemoWorkspace.Id, UserId = sarahUser.Id, Type = "ticket_summary", DeliveryMethod = "email", Priority = "low", Subject = "Daily Ticket Summary", Body = "<p>Your daily ticket summary for Acme Corporation:</p><ul><li>5 new tickets</li><li>3 resolved</li><li>2 pending your review</li></ul>", Status = "pending", CreatedAt = now },
            new Notification { WorkspaceId = tickfloDemoWorkspace.Id, UserId = mikeUser.Id, Type = "ticket_summary", DeliveryMethod = "email", Priority = "low", Subject = "Daily Ticket Summary", Body = "<p>Your daily ticket summary for Acme Corporation:</p><ul><li>3 assigned to you</li><li>1 awaiting response</li></ul>", Status = "pending", CreatedAt = now },
            failedNotification
        );
        await this.dbContext.SaveChangesAsync();

        // =============================================
        // User Notification Preferences
        // =============================================
        this.dbContext.UserNotificationPreferences.AddRange(
            new UserNotificationPreference { UserId = adminUser.Id, NotificationType = "workspace_invite", EmailEnabled = true, InAppEnabled = true },
            new UserNotificationPreference { UserId = adminUser.Id, NotificationType = "ticket_assigned", EmailEnabled = true, InAppEnabled = true },
            new UserNotificationPreference { UserId = adminUser.Id, NotificationType = "ticket_comment", EmailEnabled = true, InAppEnabled = true },
            new UserNotificationPreference { UserId = adminUser.Id, NotificationType = "ticket_status_changed", EmailEnabled = true, InAppEnabled = true },
            new UserNotificationPreference { UserId = sarahUser.Id, NotificationType = "workspace_invite", EmailEnabled = true, InAppEnabled = false },
            new UserNotificationPreference { UserId = sarahUser.Id, NotificationType = "ticket_assigned", EmailEnabled = true, InAppEnabled = true },
            new UserNotificationPreference { UserId = sarahUser.Id, NotificationType = "ticket_summary", EmailEnabled = false, InAppEnabled = true },
            new UserNotificationPreference { UserId = sarahUser.Id, NotificationType = "mention", EmailEnabled = true, InAppEnabled = true, SmsEnabled = true },
            new UserNotificationPreference { UserId = mikeUser.Id, NotificationType = "ticket_assigned", EmailEnabled = false, InAppEnabled = true },
            new UserNotificationPreference { UserId = mikeUser.Id, NotificationType = "ticket_comment", EmailEnabled = false, InAppEnabled = true },
            new UserNotificationPreference { UserId = mikeUser.Id, NotificationType = "ticket_status_changed", EmailEnabled = false, InAppEnabled = true },
            new UserNotificationPreference { UserId = mikeUser.Id, NotificationType = "mention", EmailEnabled = false, InAppEnabled = true },
            new UserNotificationPreference { UserId = lisaUser.Id, NotificationType = "workspace_invite", EmailEnabled = true, InAppEnabled = true, SmsEnabled = true, PushEnabled = true },
            new UserNotificationPreference { UserId = lisaUser.Id, NotificationType = "ticket_assigned", EmailEnabled = true, InAppEnabled = true, PushEnabled = true },
            new UserNotificationPreference { UserId = lisaUser.Id, NotificationType = "mention", EmailEnabled = true, InAppEnabled = true, SmsEnabled = true, PushEnabled = true },
            new UserNotificationPreference { UserId = lisaUser.Id, NotificationType = "password_reset", EmailEnabled = true, InAppEnabled = true, SmsEnabled = true },
            new UserNotificationPreference { UserId = tomUser.Id, NotificationType = "ticket_assigned", EmailEnabled = true, InAppEnabled = true },
            new UserNotificationPreference { UserId = tomUser.Id, NotificationType = "ticket_comment", EmailEnabled = false, InAppEnabled = true },
            new UserNotificationPreference { UserId = tomUser.Id, NotificationType = "report_completed", EmailEnabled = true, InAppEnabled = false },
            new UserNotificationPreference { UserId = tomUser.Id, NotificationType = "ticket_summary", EmailEnabled = true, InAppEnabled = false }
        );
        await this.dbContext.SaveChangesAsync();
    }

    private async Task DeleteDemoDataAsync()
    {
        var demoWorkspaceIds = await this.dbContext.Workspaces
            .Where(w => DemoWorkspaceSlugs.Contains(w.Slug))
            .Select(w => w.Id)
            .ToListAsync();

        var demoUserIds = await this.dbContext.Users
            .Where(u => DemoUserEmails.Contains(u.Email))
            .Select(u => u.Id)
            .ToListAsync();

        if (demoWorkspaceIds.Count > 0)
        {
            var demoTicketIds = await this.dbContext.Tickets
                .Where(t => demoWorkspaceIds.Contains(t.WorkspaceId))
                .Select(t => t.Id)
                .ToListAsync();

            var demoReportIds = await this.dbContext.Reports
                .Where(r => demoWorkspaceIds.Contains(r.WorkspaceId))
                .Select(r => r.Id)
                .ToListAsync();

            var demoTeamIds = await this.dbContext.Teams
                .Where(t => demoWorkspaceIds.Contains(t.WorkspaceId))
                .Select(t => t.Id)
                .ToListAsync();

            await this.dbContext.Notifications
                .Where(n => n.WorkspaceId != null && demoWorkspaceIds.Contains(n.WorkspaceId.Value))
                .ExecuteDeleteAsync();

            if (demoReportIds.Count > 0)
            {
                await this.dbContext.ReportRuns
                    .Where(rr => demoReportIds.Contains(rr.ReportId))
                    .ExecuteDeleteAsync();
            }

            await this.dbContext.Reports
                .Where(r => demoWorkspaceIds.Contains(r.WorkspaceId))
                .ExecuteDeleteAsync();

            await this.dbContext.TicketHistory
                .Where(th => demoWorkspaceIds.Contains(th.WorkspaceId))
                .ExecuteDeleteAsync();

            if (demoTicketIds.Count > 0)
            {
                await this.dbContext.TicketComments
                    .Where(tc => demoTicketIds.Contains(tc.TicketId))
                    .ExecuteDeleteAsync();

                await this.dbContext.TicketInventories
                    .Where(ti => demoTicketIds.Contains(ti.TicketId))
                    .ExecuteDeleteAsync();
            }

            await this.dbContext.Tickets
                .Where(t => demoWorkspaceIds.Contains(t.WorkspaceId))
                .ExecuteDeleteAsync();

            await this.dbContext.Inventory
                .Where(i => demoWorkspaceIds.Contains(i.WorkspaceId))
                .ExecuteDeleteAsync();

            await this.dbContext.ContactLocations
                .Where(cl => demoWorkspaceIds.Contains(cl.WorkspaceId))
                .ExecuteDeleteAsync();

            await this.dbContext.Contacts
                .Where(c => demoWorkspaceIds.Contains(c.WorkspaceId))
                .ExecuteDeleteAsync();

            if (demoTeamIds.Count > 0)
            {
                await this.dbContext.TeamMembers
                    .Where(tm => demoTeamIds.Contains(tm.TeamId))
                    .ExecuteDeleteAsync();
            }

            await this.dbContext.Teams
                .Where(t => demoWorkspaceIds.Contains(t.WorkspaceId))
                .ExecuteDeleteAsync();

            await this.dbContext.TicketStatuses
                .Where(ts => demoWorkspaceIds.Contains(ts.WorkspaceId))
                .ExecuteDeleteAsync();

            await this.dbContext.TicketPriorities
                .Where(tp => demoWorkspaceIds.Contains(tp.WorkspaceId))
                .ExecuteDeleteAsync();

            await this.dbContext.TicketTypes
                .Where(tt => demoWorkspaceIds.Contains(tt.WorkspaceId))
                .ExecuteDeleteAsync();

            await this.dbContext.Locations
                .Where(l => demoWorkspaceIds.Contains(l.WorkspaceId))
                .ExecuteDeleteAsync();

            await this.dbContext.UserWorkspaceRoles
                .Where(uwr => demoWorkspaceIds.Contains(uwr.WorkspaceId))
                .ExecuteDeleteAsync();

            await this.dbContext.RolePermissions
                .Where(rp => this.dbContext.Roles
                    .Where(r => demoWorkspaceIds.Contains(r.WorkspaceId))
                    .Select(r => r.Id)
                    .Contains(rp.RoleId))
                .ExecuteDeleteAsync();

            await this.dbContext.Roles
                .Where(r => demoWorkspaceIds.Contains(r.WorkspaceId))
                .ExecuteDeleteAsync();

            await this.dbContext.UserWorkspaces
                .Where(uw => demoWorkspaceIds.Contains(uw.WorkspaceId))
                .ExecuteDeleteAsync();

            await this.dbContext.Workspaces
                .Where(w => DemoWorkspaceSlugs.Contains(w.Slug))
                .ExecuteDeleteAsync();
        }

        if (demoUserIds.Count > 0)
        {
            await this.dbContext.UserNotificationPreferences
                .Where(unp => demoUserIds.Contains(unp.UserId))
                .ExecuteDeleteAsync();

            await this.dbContext.Tokens
                .Where(t => demoUserIds.Contains(t.UserId))
                .ExecuteDeleteAsync();

            await this.dbContext.Emails
                .Where(e =>
                    DemoUserEmails.Contains(e.To) ||
                    (e.CreatedBy != null && demoUserIds.Contains(e.CreatedBy.Value)) ||
                    (e.UpdatedBy != null && demoUserIds.Contains(e.UpdatedBy.Value)))
                .ExecuteDeleteAsync();

            await this.dbContext.UserEmailChanges
                .Where(uec => demoUserIds.Contains(uec.UserId))
                .ExecuteDeleteAsync();

            await this.dbContext.Users
                .Where(u => DemoUserEmails.Contains(u.Email))
                .ExecuteDeleteAsync();
        }
    }
}
