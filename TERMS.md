# VePF Terms of Use & Licensing Policy

**Last Updated:** November 2026  
**Website:** [https://www.vepf.net](https://www.vepf.net)

These Terms of Use ("Terms") govern your access to [www.vepf.net](https://www.vepf.net), the VePF license portal, and your acquisition and use of VePF license files (`vepf_license.ini`). Software installation and runtime redistribution are additionally governed by the [VePF End User License Agreement](./LICENSE.txt).

---

## 1. Licensing Tiers & Pricing

VePF offers two licensing tiers designed to support both individual developers and commercial teams:

### Personal Tier (Free Forever)
* **Eligibility:** Individual developers, students, educators, and open-source or hobbyist contributors working on non-commercial projects.
* **Cost:** **$0 (100% free forever)**.
* **Scope:** Full, uninterrupted runtime sessions on activated personal development and testing machines. Does not permit commercial distribution or use within a for-profit organization.

### Commercial Tier (Early Adopter Program)
* **Eligibility:** Businesses, startups, agencies, independent software vendors (ISVs), and commercial developers building proprietary or commercial software.
* **Early Adopter Offer:** Any commercial license acquired via [www.vepf.net](https://www.vepf.net) **before March 1, 2027**, is **100% free** and grants royalty-free runtime redistribution rights under Section 3.2 of the [EULA](./LICENSE.txt).
* **Post-Promotion Licensing:** Commercial licenses acquired on or after **March 1, 2027**, will be subject to the commercial pricing published on [www.vepf.net](https://www.vepf.net) at the time of acquisition. Licenses issued prior to March 1, 2027, remain valid under their original grant terms.

---

## 2. Machine Fingerprinting & Privacy

To issue an offline `vepf_license.ini` file without requiring constant internet connectivity at runtime, `VepfLicenseTool` generates a deterministic hardware fingerprint of the target machine.

* **What is collected:** A one-way cryptographic hash derived from non-personal system hardware and OS machine identifiers.
* **What is NOT collected:** VePF does not collect your source code, application binaries, end-user data, file contents, or runtime telemetry.
* **Storage:** Machine fingerprints submitted to [www.vepf.net](https://www.vepf.net) are used solely to generate and manage your `vepf_license.ini` file.

---

## 3. Hardware Changes & Re-Activation

If you reinstall your Linux operating system, upgrade hardware, or migrate to a new development machine, you may generate a new machine fingerprint with `./VepfLicenseTool` and request an updated `vepf_license.ini` file through [www.vepf.net](https://www.vepf.net) under your existing license entitlement.

---

## 4. Acceptable Use

When using [www.vepf.net](https://www.vepf.net) and the VePF licensing service, you agree not to:

1. Submit automated bulk requests to scrape or exhaust license generation endpoints.
2. Share, publish, or resell `vepf_license.ini` files issued to your organization with unauthorized third parties (except as embedded within your compiled end-user applications as permitted by the EULA).
3. Attempt to reverse engineer, forge, or tamper with license signature validation.

---

## 5. Updates, Support & Disclaimers

* **Updates:** VePF may release periodic updates to improve Linux distribution compatibility, rendering performance, and WPF API coverage.
* **Support:** Community support, issue reporting, and documentation are provided via [www.vepf.net](https://www.vepf.net) and our official project repository.
* **No Warranty:** Services on [www.vepf.net](https://www.vepf.net) and all VePF software downloads are provided "as is" without warranties of uninterrupted availability or fitness for a particular purpose.

---

## 6. Changes to These Terms

We may update these Terms from time to time to reflect new features, pricing updates after the early adopter period, or legal requirements. Material changes will be posted on [www.vepf.net](https://www.vepf.net) with an updated revision date.