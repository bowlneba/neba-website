using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class MakeContactPhoneTypeRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // contact_phone_type is nullable by design: EF's owned-type table splitting for the
            // optional SponsorContact uses a null required property to mean "no contact" (one
            // sponsor genuinely has none). The bug wasn't that nullability - it's that 28 sponsors
            // had a contact_name and a real phone number but a null contact_phone_type, because they
            // were imported outside the domain's SponsorFieldBuilder.BuildSponsorContact factory,
            // which always requires a phone type. The API's null-omitting JSON serializer then
            // silently dropped contact.phone.phoneNumberType from GetSponsorDetail responses,
            // breaking client deserialization against the contract's required PhoneNumberType field
            // for every authenticated (sponsor-management) request to one of those sponsors.
            //
            // Backfill to Work ('W') - matching these sponsors' other, correctly-typed phone numbers.
            migrationBuilder.Sql(
                """
                UPDATE app.sponsors
                SET contact_phone_type = 'W'
                WHERE contact_name IS NOT NULL
                  AND contact_phone_type IS NULL
                  AND contact_phone_number IS NOT NULL;
                """);

            // One sponsor (tournament-sense) has a contact_name but no phone/email at all - not a
            // usable contact, just a name with nothing to back it up. Clear it entirely rather than
            // invent phone/email data that doesn't exist, so it's correctly treated as "no contact"
            // like the one sponsor that already had every contact field null.
            migrationBuilder.Sql(
                """
                UPDATE app.sponsors
                SET contact_name = NULL,
                    contact_phone_type = NULL,
                    contact_phone_country_code = NULL,
                    contact_phone_number = NULL,
                    contact_phone_extension = NULL,
                    contact_email_address = NULL
                WHERE contact_name IS NOT NULL AND contact_phone_number IS NULL;
                """);

            // Enforce the actual invariant going forward: a phone type is required whenever a
            // contact is present, without forcing the column NOT NULL outright (that would break
            // the legitimate "sponsor has no contact" case).
            migrationBuilder.Sql(
                """
                ALTER TABLE app.sponsors
                ADD CONSTRAINT ck_sponsors_contact_phone_type_required_when_contact_present
                CHECK (contact_name IS NULL OR contact_phone_type IS NOT NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE app.sponsors
                DROP CONSTRAINT ck_sponsors_contact_phone_type_required_when_contact_present;
                """);

            // Data backfilled/cleared in Up is not reversed - there is no prior state to restore to.
        }
    }
}
