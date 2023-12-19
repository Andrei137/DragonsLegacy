### Achievement
    Id: int, Key
    Name: string, Required, MaxLength(25)
    Description: string, Required, MaxLength(100)
    ExperiencePoints: int, Required
    Coins: int, Default(0)
    UserAchievements: ICollection<UserAchievement>?

### ApplicationUser (IdentityUser)
    FirstName: string?
    LastName: string?
    BirthDate: DateTime?
    ExperiencePoints: int, Default(0)
    Coins: int, Default(0)
    Comments: ICollection<Comment>?
    Tasks: ICollection<Task>?
    TeamsHistory: ICollection<TeamHistory>?
    UserAchievements: ICollection<UserAchievement>?
    UserItems: ICollection<UserItem>?
    UserTeams: ICollection<UserTeam>?
    UserProjects: ICollection<UserProject>?

### Category
    Id: int, Key
    Name: string, Required, MaxLength(15)
    TaskCategories: ICollection<TaskCategory>?

### Comments
    Id: int, Key
    Content: string, Required
    Date: DateTime
    UserId: string
    TaskId: int
    User: ApplicationUser?
    Task: Task?

### Item
    Id: int, Key
    Name: string, Required, MaxLength(50)
    Description: string?
    Price: int, Required
    NumberOfItems: int, Required
    UserItems: ICollection<UserItem>?

### Project
    Id: int, Key
    Name: string, Required, MaxLength(25)
    Description: string?
    OrganizerId: string?
    Organizer: ApplicationUser?
    TeamProjects: ICollection<TeamProject>?
    UserProjects: ICollection<UserProject>?

### Task
    Id: int, Key
    Name: string, Required, MaxLength(25)
    Description: string?
    Priority: int, Required
    Status: string, Required
    Deadline: DateTime, Required
    StartDate: DateTime, Required
    EndDate: DateTime?
    Multimedia: string?
    ExperiencePoints: int, Required
    Coins: int, Default(0)
    UserId: string
    User: ApplicationUser?
    Comments: ICollection<Comment>?
    TaskCategories: ICollection<TaskCategory>?

### TaskCategory
    TaskId: int, Key
    CategoryId: int, Key
    Task: Task?
    Category: Category?

### Team
    Id: int, Key
    Name: string, Required, MaxLength(25)
    Description: string?
    ManagerId: string?
    Manager: ApplicationUser?
    TeamsHistory: ICollection<TeamHistory>?
    TeamProjects: ICollection<TeamProject>?
    UserTeams: ICollection<UserTeam>?

### TeamHistory
    UserId: string, Key
    StartDate: DateTime, Key
    TeamId: int
    EndDate: DateTime
    User: ApplicationUser?
    Team: Team?

### TeamProject
    TeamId: int, Key
    ProjectId: int, Key
    Team: Team?
    Project: Project?

### UserAchievement
    UserId: string, Key
    AchievementId: int, Key
    UnlockDate: DateTime
    User: ApplicationUser?
    Achievement: Achievement?

### UserItem
    UserId: string, Key
    ItemId: int, Key
    PurchaseDate: DateTime
    User: ApplicationUser?
    Item: Item?

### UserProject
    UserId: string, Key
    ProjectId: int, Key
    User: ApplicationUser?
    Project: Project?

### UsersTeams
    UserId: string, Key
    TeamId: int, Key
    User: ApplicationUser?
    Team: Team?

