import { z } from "zod";

const EMAIL_MAX_LENGTH = 254;
const EMAIL_LOCAL_MAX_LENGTH = 64;
const EMAIL_DOMAIN_MAX_LENGTH = 253;
const EMAIL_LABEL_MAX_LENGTH = 63;

function hasValidEmailDomain(email: string) {
  const domain = email.split("@")[1] ?? "";
  const labels = domain.split(".");
  const tld = labels.at(-1) ?? "";

  return (
    domain.length <= EMAIL_DOMAIN_MAX_LENGTH &&
    labels.length >= 2 &&
    labels.every(
      (label) =>
        label.length > 0 &&
        label.length <= EMAIL_LABEL_MAX_LENGTH &&
        !label.startsWith("-") &&
        !label.endsWith("-")
    ) &&
    /^[a-z]{2,63}$/i.test(tld)
  );
}

function hasValidEmailLocalPart(email: string) {
  const localPart = email.split("@")[0] ?? "";

  return (
    localPart.length > 0 &&
    localPart.length <= EMAIL_LOCAL_MAX_LENGTH &&
    !localPart.startsWith(".") &&
    !localPart.endsWith(".")
  );
}

export const emailValidationSchema = z
  .string()
  .trim()
  .toLowerCase()
  .min(1, "Email is required")
  .max(EMAIL_MAX_LENGTH, "Email must not exceed 254 characters")
  .email("Enter a valid email address")
  .refine((email) => !/\s/.test(email), "Email must not contain spaces")
  .refine((email) => !email.includes(".."), "Email cannot contain consecutive dots")
  .refine(hasValidEmailLocalPart, "Enter a valid email address")
  .refine(hasValidEmailDomain, "Enter a valid email domain");
