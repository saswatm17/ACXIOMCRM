using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Constants;

namespace AcxiomCRM.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndUsersAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            string[] roles = { AppRoles.Admin, AppRoles.Manager, AppRoles.SalesExecutive };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Admin User
            const string adminEmail = "admin@acxiomcrm.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Rajesh Sharma",
                    PhoneNumber = "9810123456",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@12345");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
                }
            }

            // Seed Manager User
            const string managerEmail = "manager@acxiomcrm.com";
            var managerUser = await userManager.FindByEmailAsync(managerEmail);
            if (managerUser == null)
            {
                managerUser = new ApplicationUser
                {
                    UserName = managerEmail,
                    Email = managerEmail,
                    FullName = "Priya Patel",
                    PhoneNumber = "9820123456",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(managerUser, "Manager@12345");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, AppRoles.Manager);
                }
            }

            // Seed Sales Executive User
            const string salesEmail = "sales@acxiomcrm.com";
            var salesUser = await userManager.FindByEmailAsync(salesEmail);
            if (salesUser == null)
            {
                salesUser = new ApplicationUser
                {
                    UserName = salesEmail,
                    Email = salesEmail,
                    FullName = "Amit Verma",
                    PhoneNumber = "9830123456",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(salesUser, "Sales@12345");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(salesUser, AppRoles.SalesExecutive);
                }
            }

            // Seed Realistic Indian Customer Data
            if (!await context.Customers.AnyAsync())
            {
                var customers = new List<Customer>
                {
                    new Customer
                    {
                        CustomerCode = "CUST-1001",
                        CustomerName = "Aditya Birla Renewables",
                        Email = "ravi.narayanan@adityabirla.com",
                        Phone = "9845012345",
                        CompanyName = "Aditya Birla Group",
                        Address = "Aditya Birla Centre, S.K. Ahire Marg, Worli",
                        City = "Mumbai",
                        State = "Maharashtra",
                        Status = CustomerStatus.Active,
                        CreatedDate = DateTime.UtcNow.AddDays(-25),
                        CreatedBy = adminUser.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1002",
                        CustomerName = "TCS Digital Practice",
                        Email = "meera.kulkarni@tcs.com",
                        Phone = "9876512345",
                        CompanyName = "Tata Consultancy Services",
                        Address = "Sahyadri Park, Rajiv Gandhi Infotech Park, Hinjewadi",
                        City = "Pune",
                        State = "Maharashtra",
                        Status = CustomerStatus.Active,
                        CreatedDate = DateTime.UtcNow.AddDays(-18),
                        CreatedBy = managerUser.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1003",
                        CustomerName = "Infosys Cloud Infra",
                        Email = "karan.singhania@infosys.com",
                        Phone = "9823412345",
                        CompanyName = "Infosys Technologies Ltd",
                        Address = "44 Electronics City, Hosur Road",
                        City = "Bengaluru",
                        State = "Karnataka",
                        Status = CustomerStatus.Active,
                        CreatedDate = DateTime.UtcNow.AddDays(-10),
                        CreatedBy = salesUser.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1004",
                        CustomerName = "Zomato Logistics Hub",
                        Email = "ananya.roy@zomato.com",
                        Phone = "9811212345",
                        CompanyName = "Zomato Media Pvt Ltd",
                        Address = "Ground Floor, Tower C, Pioneer Urban Square, Golf Course Extension",
                        City = "Gurugram",
                        State = "Haryana",
                        Status = CustomerStatus.Active,
                        CreatedDate = DateTime.UtcNow.AddDays(-4),
                        CreatedBy = salesUser.Id
                    }
                };

                context.Customers.AddRange(customers);
                await context.SaveChangesAsync();
            }

            // Seed Realistic Indian Leads
            if (!await context.Leads.AnyAsync())
            {
                var leads = new List<Lead>
                {
                    new Lead
                    {
                        LeadCode = "LEAD-1001",
                        LeadName = "Vikram Malhotra",
                        Email = "vikram.malhotra@apexhealth.in",
                        Phone = "9812345678",
                        CompanyName = "Apex Health Systems",
                        Source = LeadSource.Website,
                        Status = LeadStatus.Qualified,
                        ExpectedValue = 250000,
                        CreatedDate = DateTime.UtcNow.AddDays(-15),
                        AssignedTo = salesUser.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1002",
                        LeadName = "Sunita Deshmukh",
                        Email = "sunita.d@fintechwave.com",
                        Phone = "9822234567",
                        CompanyName = "FintechWave Solutions",
                        Source = LeadSource.Referral,
                        Status = LeadStatus.Contacted,
                        ExpectedValue = 450000,
                        CreatedDate = DateTime.UtcNow.AddDays(-8),
                        AssignedTo = salesUser.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1003",
                        LeadName = "Harish Chandra",
                        Email = "harish.c@bharatlogistics.org",
                        Phone = "9833345678",
                        CompanyName = "Bharat Logistics Network",
                        Source = LeadSource.Event,
                        Status = LeadStatus.New,
                        ExpectedValue = 180000,
                        CreatedDate = DateTime.UtcNow.AddDays(-3),
                        AssignedTo = managerUser.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1004",
                        LeadName = "Deepak Singhal",
                        Email = "deepak@singhalauto.in",
                        Phone = "9844456789",
                        CompanyName = "Singhal Auto Components",
                        Source = LeadSource.ColdCall,
                        Status = LeadStatus.Qualified,
                        ExpectedValue = 600000,
                        CreatedDate = DateTime.UtcNow.AddDays(-12),
                        AssignedTo = adminUser.Id
                    }
                };

                context.Leads.AddRange(leads);
                await context.SaveChangesAsync();
            }

            // Seed Realistic Indian Opportunities
            if (!await context.Opportunities.AnyAsync())
            {
                var customers = await context.Customers.OrderBy(c => c.CustomerId).Take(4).ToListAsync();
                if (customers.Count >= 4)
                {
                    var opportunities = new List<Opportunity>
                    {
                        new Opportunity
                        {
                            OpportunityName = "Birla Solar Farm SCADA Solution",
                            CustomerId = customers[0].CustomerId,
                            Amount = 1200000,
                            Stage = OpportunityStage.Proposal,
                            Probability = 40,
                            Status = OpportunityStatus.Open,
                            ExpectedCloseDate = DateTime.UtcNow.AddDays(45),
                            CreatedDate = DateTime.UtcNow.AddDays(-14),
                            AssignedTo = adminUser.Id
                        },
                        new Opportunity
                        {
                            OpportunityName = "TCS Banking Microservices Upgrade",
                            CustomerId = customers[1].CustomerId,
                            Amount = 2800000,
                            Stage = OpportunityStage.Negotiation,
                            Probability = 75,
                            Status = OpportunityStatus.Open,
                            ExpectedCloseDate = DateTime.UtcNow.AddDays(20),
                            CreatedDate = DateTime.UtcNow.AddDays(-20),
                            AssignedTo = managerUser.Id
                        },
                        new Opportunity
                        {
                            OpportunityName = "Infosys Hybrid Cloud Orchestrator",
                            CustomerId = customers[2].CustomerId,
                            Amount = 850000,
                            Stage = OpportunityStage.Won,
                            Probability = 100,
                            Status = OpportunityStatus.Won,
                            ExpectedCloseDate = DateTime.UtcNow.AddDays(-5),
                            CreatedDate = DateTime.UtcNow.AddDays(-30),
                            AssignedTo = salesUser.Id
                        },
                        new Opportunity
                        {
                            OpportunityName = "Zomato Cold Chain GPS Tracking",
                            CustomerId = customers[3].CustomerId,
                            Amount = 350000,
                            Stage = OpportunityStage.Qualification,
                            Probability = 20,
                            Status = OpportunityStatus.Open,
                            ExpectedCloseDate = DateTime.UtcNow.AddDays(60),
                            CreatedDate = DateTime.UtcNow.AddDays(-4),
                            AssignedTo = salesUser.Id
                        }
                    };

                    context.Opportunities.AddRange(opportunities);
                    await context.SaveChangesAsync();
                }
            }

            // Seed Realistic Indian Follow-Ups
            if (!await context.FollowUps.AnyAsync())
            {
                var customers = await context.Customers.OrderBy(c => c.CustomerId).Take(2).ToListAsync();
                var leads = await context.Leads.OrderBy(l => l.LeadId).Take(2).ToListAsync();

                var followUps = new List<FollowUp>();

                if (leads.Count >= 2)
                {
                    followUps.Add(new FollowUp
                    {
                        LeadId = leads[0].LeadId,
                        FollowUpType = FollowUpType.ProductDemo,
                        FollowUpDate = DateTime.UtcNow.AddDays(2).Date.AddHours(11), // 2 days from now at 11:00 AM
                        Remarks = "Deliver live product demo of AcxiomCRM lead pipeline and custom field reporting for Apex Health.",
                        Status = FollowUpStatus.Planned,
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-2)
                    });

                    followUps.Add(new FollowUp
                    {
                        LeadId = leads[1].LeadId,
                        FollowUpType = FollowUpType.Call,
                        FollowUpDate = DateTime.UtcNow.AddDays(-1).Date.AddHours(15), // Yesterday 3:00 PM (Overdue/Missed)
                        Remarks = "Introductory qualification call regarding FintechWave integration requirements.",
                        Status = FollowUpStatus.Missed,
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-5)
                    });
                }

                if (customers.Count >= 2)
                {
                    followUps.Add(new FollowUp
                    {
                        CustomerId = customers[0].CustomerId,
                        FollowUpType = FollowUpType.Meeting,
                        FollowUpDate = DateTime.UtcNow.AddDays(-3).Date.AddHours(14),
                        Remarks = "Quarterly business review meeting at TCS Mumbai campus. Client agreed to renew enterprise support contract.\n[Completed on 09-Oct-2026]: Contract finalized.",
                        Status = FollowUpStatus.Completed,
                        AssignedTo = adminUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-10)
                    });

                    followUps.Add(new FollowUp
                    {
                        CustomerId = customers[1].CustomerId,
                        FollowUpType = FollowUpType.Call,
                        FollowUpDate = DateTime.UtcNow.Date.AddHours(16), // Due Today at 4:00 PM
                        Remarks = "Check in on user onboarding feedback and answer question on CSV export.",
                        Status = FollowUpStatus.Planned,
                        AssignedTo = managerUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-1)
                    });
                }

                if (followUps.Any())
                {
                    context.FollowUps.AddRange(followUps);
                    await context.SaveChangesAsync();
                }
            }

            // Seed Realistic Indian Activities
            if (!await context.Activities.AnyAsync())
            {
                var customers = await context.Customers.OrderBy(c => c.CustomerId).Take(2).ToListAsync();
                var leads = await context.Leads.OrderBy(l => l.LeadId).Take(2).ToListAsync();

                var activities = new List<Activity>();

                if (leads.Count >= 2)
                {
                    activities.Add(new Activity
                    {
                        LeadId = leads[0].LeadId,
                        ActivityType = ActivityType.Call,
                        Subject = "Introductory Discovery Call - Cloud Pipeline Needs",
                        Description = "Connected with Vikram Malhotra. Discussed Apex Health's expansion across 14 diagnostic clinics and need for centralized lead conversion tracking.",
                        ActivityDate = DateTime.UtcNow.AddDays(-6).Date.AddHours(11),
                        Status = ActivityStatus.Completed,
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-6)
                    });

                    activities.Add(new Activity
                    {
                        LeadId = leads[1].LeadId,
                        ActivityType = ActivityType.Task,
                        Subject = "Draft Integration Architecture Document",
                        Description = "Compile technical specifications for FintechWave REST API integration with payment gateway webhooks.",
                        ActivityDate = DateTime.UtcNow.AddDays(1).Date.AddHours(17),
                        Status = ActivityStatus.Pending,
                        AssignedTo = salesUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-1)
                    });
                }

                if (customers.Count >= 2)
                {
                    activities.Add(new Activity
                    {
                        CustomerId = customers[0].CustomerId,
                        ActivityType = ActivityType.Meeting,
                        Subject = "Executive Quarterly Steering Committee",
                        Description = "Met with TCS leadership at Mumbai HQ. Presented system throughput benchmarks and secured sign-off for annual maintenance upgrade.",
                        ActivityDate = DateTime.UtcNow.AddDays(-3).Date.AddHours(15),
                        Status = ActivityStatus.Completed,
                        AssignedTo = adminUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-3)
                    });

                    activities.Add(new Activity
                    {
                        CustomerId = customers[1].CustomerId,
                        ActivityType = ActivityType.Email,
                        Subject = "Dispatched User Manual & Onboarding Training Guide",
                        Description = "Sent customized PDF documentation and Loom walkthrough links to Infosys Bengaluru engineering team.",
                        ActivityDate = DateTime.UtcNow.AddDays(-1).Date.AddHours(10),
                        Status = ActivityStatus.Completed,
                        AssignedTo = managerUser.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-1)
                    });
                }

                if (activities.Any())
                {
                    context.Activities.AddRange(activities);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
