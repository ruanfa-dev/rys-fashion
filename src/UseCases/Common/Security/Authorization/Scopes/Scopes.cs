namespace UseCases.Common.Security.Authorization.Scopes;

/// <summary>
/// Defines application scopes for Keycloak authorization
/// Scopes represent the actions that can be performed on resources
/// </summary>
public static class Scopes
{
    // CRUD Operations
    public const string Create = "create";
    public const string Read = "read";
    public const string Update = "update";
    public const string Delete = "delete";

    // Specialized Operations
    public const string List = "list";
    public const string Search = "search";
    public const string View = "view";
    public const string Edit = "edit";

    // Management Operations
    public const string Manage = "manage";
    public const string Administer = "administer";
    public const string Configure = "configure";

    // Approval and Workflow Operations
    public const string Approve = "approve";
    public const string Reject = "reject";
    public const string Submit = "submit";
    public const string Review = "review";

    // Publishing and Status Operations
    public const string Publish = "publish";
    public const string Unpublish = "unpublish";
    public const string Enable = "enable";
    public const string Disable = "disable";

    // Import/Export Operations
    public const string Import = "import";
    public const string Export = "export";
    public const string Backup = "backup";
    public const string Restore = "restore";

    // Notification and Communication Operations
    public const string Notify = "notify";
    public const string Send = "send";
    public const string Receive = "receive";

    /// <summary>
    /// Gets all available scopes
    /// </summary>
    public static readonly string[] All = 
    [
        Create, Read, Update, Delete,
        List, Search, View, Edit,
        Manage, Administer, Configure,
        Approve, Reject, Submit, Review,
        Publish, Unpublish, Enable, Disable,
        Import, Export, Backup, Restore,
        Notify, Send, Receive
    ];

    /// <summary>
    /// Gets basic CRUD scopes
    /// </summary>
    public static readonly string[] BasicCrud = [Create, Read, Update, Delete];

    /// <summary>
    /// Gets read-only scopes
    /// </summary>
    public static readonly string[] ReadOnly = [Read, List, Search, View];

    /// <summary>
    /// Gets administrative scopes
    /// </summary>
    public static readonly string[] Administrative = [Manage, Administer, Configure];

    /// <summary>
    /// Gets workflow scopes
    /// </summary>
    public static readonly string[] Workflow = [Approve, Reject, Submit, Review];

    /// <summary>
    /// Gets publishing scopes
    /// </summary>
    public static readonly string[] Publishing = [Publish, Unpublish, Enable, Disable];

    /// <summary>
    /// Gets data management scopes
    /// </summary>
    public static readonly string[] DataManagement = [Import, Export, Backup, Restore];
}