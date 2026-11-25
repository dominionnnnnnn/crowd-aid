export const relationships = [
  "Parent",
  "Child",
  "Spouse",
  "Sibling",
  "Aunt or uncle",
  "Grandparent",
  "Friend",
  "Neighbour",
  "Community or religious leader",
  "Other",
];
export const lgas = [
  "Abeokuta North",
  "Abeokuta South",
  "Ado-Odo/Ota",
  "Ewekoro",
  "Ifo",
  "Ijebu East",
  "Ijebu North",
  "Ijebu North East",
  "Ijebu Ode",
  "Ikenne",
  "Imeko Afon",
  "Ipokia",
  "Obafemi Owode",
  "Odeda",
  "Odogbolu",
  "Ogun Waterside",
  "Remo North",
  "Sagamu",
  "Yewa North",
  "Yewa South",
];
export const supportTypes = [
  "Financial support",
  "Transportation",
  "Medical equipment",
  "Food",
  "Blood donor coordination",
  "Professional support",
];
export const needs = [
  "Transport to hospital",
  "Blood donors",
  "Meals for family",
  "Professional support",
  "Medical equipment",
];
export const availability = ["Weekdays", "Weekends", "Flexible"];
export const areas = [
  "Adatan",
  "Asero",
  "Camp",
  "Ibara",
  "Isale-Igbein",
  "Kuto",
  "Lafenwa",
  "Obantoko",
  "Oke-Ilewo",
  "Onikolobo",
  "Panseke",
  "Somewhere else",
];
export const outreachRoles = [
  "Collecting donations",
  "Sharing campaign stories",
  "Setup & logistics",
  "Wherever needed",
];
export const interests = [
  "Collecting donations at outreaches",
  "Sharing campaign stories",
  "Transport and logistics",
  "Healthcare or professional skills",
  "Admin and coordination",
];
export const helpCategories = [
  {
    value: "Transportation",
    icon: "transport",
    description: "Drive a patient or family to appointments",
  },
  {
    value: "Blood donor coordination",
    icon: "blood",
    description: "Be contacted when a patient needs donors",
  },
  {
    value: "Medical equipment",
    icon: "equipment",
    description: "Lend or give equipment like wheelchairs",
  },
  {
    value: "Food support",
    icon: "food",
    description: "Provide meals for families during hospital stays",
  },
  {
    value: "Professional assistance",
    icon: "professional",
    description: "Offer skills such as nursing, counselling or legal help",
  },
  {
    value: "Other support",
    icon: "other",
    description: "Something else you can offer",
  },
];
export const needToCategory = (need) =>
  ({
    "Transport to hospital": "Transportation",
    "Blood donors": "Blood donor coordination",
    "Meals for family": "Food support",
    "Professional support": "Professional assistance",
    "Medical equipment": "Medical equipment",
  })[need] || "Other support";
export const validEmail = (value) =>
  /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value || "");
export const validPhone = (value) =>
  /^(?:0|\+?234)\d{10}$/.test((value || "").replace(/[\s()-]/g, ""));
export const wholeAmount = (value, min = 0) =>
  value !== "" && Number.isSafeInteger(Number(value)) && Number(value) >= min;
export function requiredFields(values, keys) {
  return Object.fromEntries(
    keys
      .filter((key) =>
        Array.isArray(values[key])
          ? !values[key].length
          : typeof values[key] === "boolean"
            ? !values[key]
            : !String(values[key] ?? "").trim(),
      )
      .map((key) => [key, "This field is required."]),
  );
}
export function contactErrors(values, withEmail = false) {
  const fields = requiredFields(values, ["name", "phone"]);
  if (!validPhone(values.phone))
    fields.phone = "Enter a Nigerian number, e.g. 0803 123 4567.";
  if (withEmail && !validEmail(values.email))
    fields.email = "Enter a valid email address.";
  return fields;
}
