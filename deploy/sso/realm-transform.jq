# Injects the Azure deployment specifics into the canonical OrderSphere realm.
# $bff is passed via: jq --arg bff "<BFF_BASE_URL>" -f realm-transform.jq
# Keeps contracts/keycloak/ordersphere-realm.json localhost-clean for local Aspire.
#
# If $bff is empty (BFF Azure URL not known yet), only sslRequired is changed; the
# web-bff redirect/logout URLs stay at localhost and can be added later in the admin
# console once the BFF is deployed.
.sslRequired = "external"
| if ($bff | length) > 0 then
    (.clients[] | select(.clientId == "web-bff")) |= (
      .redirectUris += [$bff + "/*"]
      | .attributes["backchannel.logout.url"] = ($bff + "/bff/backchannel-logout")
      | .attributes["post.logout.redirect.uris"] = (.attributes["post.logout.redirect.uris"] + "##" + $bff + "/*")
    )
  else . end
