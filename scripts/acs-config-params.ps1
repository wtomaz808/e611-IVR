# ─────────────────────────────────────────────────────────────────
# ACS Custom Domain Configuration Parameters
# E911 IVR Development Environment
# ─────────────────────────────────────────────────────────────────

# Azure Resource Information
$AcsParams = @{
    ResourceGroup  = "rg-ivr-dev"
    AcsResourceName = "ivr-dev-acs-bld64pwxb4ukq"
    CustomDomain   = "acs.tomazdev.us"
}

# Optional: Azure DNS Configuration (uncomment if using Azure DNS)
# $AcsParams.DnsResourceGroup = "dns-rg"
# $AcsParams.DnsZoneName = "devtest.us"
# $AcsParams.AutoAddDnsRecords = $true

# Return parameters for script execution
return $AcsParams
