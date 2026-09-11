export function round2(value) {
  // Number.prototype.toFixed rounds the *actual* IEEE-754 stored value correctly
  // (per the ECMAScript spec's algorithm). A tempting alternative -
  // Math.round((value + Number.EPSILON) * 100) / 100 - looks like it fixes
  // floating-point rounding, but it doesn't: it nudges every value by the same
  // fixed tiny amount regardless of magnitude, which over-corrects values that
  // are genuinely stored just *below* a decimal boundary (e.g. the literal 0.235
  // is actually stored as 0.23499999999999998668, so the mathematically correct
  // rounding is 0.23 - the epsilon trick wrongly produces 0.24 instead).
  return Number(value.toFixed(2))
}

export function calculateLineTotals(quantity, unitPrice, taxRate) {
  const lineTotal = round2((quantity || 0) * (unitPrice || 0))
  const lineTax = round2((lineTotal * (taxRate || 0)) / 100)
  return { lineTotal, lineTax }
}

export function calculateInvoiceTotals(lines) {
  const subTotal = round2(lines.reduce((sum, line) => sum + (line.lineTotal || 0), 0))
  const totalTax = round2(lines.reduce((sum, line) => sum + (line.lineTax || 0), 0))
  return { subTotal, totalTax, grandTotal: round2(subTotal + totalTax) }
}
