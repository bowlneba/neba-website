using System.ComponentModel.DataAnnotations;

namespace Neba.Infrastructure.Documents;

/// <summary>
/// Represents the configuration settings for Google Drive integration, including application details, credentials, and document mappings.
/// </summary>
internal sealed record GoogleDriveSettings
{
    /// <summary>
    /// Gets the name of the application that will be used when accessing Google Drive. This is required for authentication and API access.
    /// </summary>
    [Required]
    public required string ApplicationName { get; init;}

    /// <summary>
    /// Gets the credentials required to authenticate with Google Drive. This includes the private key, client email, private key ID, and client X509 certificate URL. These credentials are necessary for secure access to the Google Drive API.
    /// </summary>
    [Required]
    public required GoogleDriveCredentials Credentials { get; init; }

    /// <summary>
    /// Gets the collection of documents that are configured for access through Google Drive. Each document includes its name, unique document ID, and the route for accessing it. At least one document must be configured to ensure that there is content available for retrieval from Google Drive.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one document must be configured.")]
    public required Document[] Documents { get; init; }
}

/// <summary>
/// Represents the credentials required for authenticating with Google Drive. This includes the private key, client email, private key ID, and client X509 certificate URL. These credentials are essential for secure access to the Google Drive API and must be provided for successful integration.
/// </summary>
internal sealed record GoogleDriveCredentials
{
    /// <summary>
    /// Google Cloud project identifier.
    /// </summary>
    [Required]
    public required string ProjectId { get; init; }

    /// <summary>
    /// Private key from the service account JSON key file.
    /// Must be in PEM format. Newline characters (\n) will be processed during initialization.
    /// </summary>
    [Required]
    public required string PrivateKey { get; set; }

    /// <summary>
    /// Service account email address.
    /// Format: service-account-name@project-id.iam.gserviceaccount.com
    /// </summary>
    [Required]
    public required string ClientEmail { get; init; }

    /// <summary>
    /// Private key identifier from the service account.
    /// </summary>
    [Required]
    public required string PrivateKeyId { get; init; }
}

/// <summary>
/// Represents a document configuration for Google Drive integration. Each document includes its name, unique document ID, and the route for accessing it. This information is essential for retrieving and managing documents from Google Drive through the API. Each document must be properly configured to ensure successful access and retrieval of content from Google Drive.
/// </summary>
internal sealed record Document
{
    /// <summary>
    /// Gets the unique document ID assigned by Google Drive. This ID is required to access the specific document through the Google Drive API. It is essential for retrieving the document's content and must be provided for each document configured in the application.
    /// </summary>
    [Required]
    public required string DocumentId { get; init; }

    /// <summary>
    /// Gets the name of the document. This is a required field that identifies the document within the application. It is used for display purposes and to reference the document when accessing it through the Google Drive API.
    /// </summary>
    [Required]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the web route for accessing the document. This is a required field that specifies the URL path through which the document can be accessed within the application. It is used to route requests to the appropriate document when users attempt to access it through the application's interface.
    /// </summary>
    [Required]
    public required string WebRoute { get; init; }
}