import { describe, expect, it } from 'vitest'
import { calculateInvoiceTotals, calculateLineTotals, round2 } from './invoiceTotals'

describe('round2', () => {
  it('rounds to 2 decimal places', () => {
    expect(round2(10)).toBe(10)
    expect(round2(10.1)).toBe(10.1)
    expect(round2(10.999)).toBe(11)
  })

  it('leaves already-2-decimal values unchanged', () => {
    expect(round2(19.99)).toBe(19.99)
    expect(round2(0.01)).toBe(0.01)
  })

  it('handles zero and negative values', () => {
    expect(round2(0)).toBe(0)
    expect(round2(-10.005)).toBeCloseTo(-10.01, 2)
    expect(round2(-19.999)).toBe(-20)
  })

  it('cleans up floating-point representation noise from addition (0.1 + 0.2)', () => {
    expect(0.1 + 0.2).not.toBe(0.3) // the underlying JS quirk this guards against
    expect(round2(0.1 + 0.2)).toBe(0.3)
  })

  it('rounds "looks like X.XX5" literals to whatever they are actually stored as', () => {
    // These are NOT stored as the exact decimal they look like in IEEE-754 double
    // precision - e.g. 1.005 is actually stored as 1.0049999999999998934..., which
    // is genuinely below the 1.005 midpoint, so the mathematically correct answer
    // is 1.00, not 1.01. (A popular but WRONG "fix" - adding Number.EPSILON before
    // rounding - makes this worse: it blindly nudges every value up regardless of
    // which side of the boundary it's actually on, which silently corrupts totals
    // for perfectly ordinary tax calculations - see the next test.)
    expect(round2(1.005)).toBe(1)
    expect(round2(1.015)).toBe(1.01)
    expect(round2(1.025)).toBe(1.02)
    expect(round2(35.855)).toBe(35.85)
    expect(round2(2.675)).toBe(2.67)
  })

  it('rounds ordinary tax-calculation results correctly (regression: the old Number.EPSILON approach got these wrong)', () => {
    // 0.5 units at 21% tax, and 1 unit at 23.5% tax, are completely unremarkable
    // real-world invoice inputs. Both land - purely as an artifact of binary
    // floating point - just below a rounding boundary. The previous
    // implementation (Math.round((value + Number.EPSILON) * 100) / 100) rounded
    // BOTH of these up incorrectly (0.11 and 0.24). Verified ground truth:
    // (0.5 * 21 / 100).toPrecision(20) === "0.10499999999999999611"
    // (1 * 23.5 / 100).toPrecision(20) === "0.23499999999999998668"
    expect(round2((0.5 * 21) / 100)).toBe(0.1)
    expect(round2((1 * 23.5) / 100)).toBe(0.23)
  })

  it('agrees with Number.prototype.toFixed across a broad sweep of realistic tax-calculation outputs', () => {
    // round2 is now implemented in terms of toFixed, so this mainly guards
    // against a future reimplementation reintroducing the epsilon-style bug.
    const rates = [6, 13, 19.6, 21, 23, 23.5, 68.5]
    const amounts = [0.5, 1, 4.49, 17.125, 19.99, 25, 33.333, 99.995, 1234.56]

    for (const rate of rates) {
      for (const amount of amounts) {
        const raw = (amount * rate) / 100
        expect(round2(raw)).toBe(Number(raw.toFixed(2)))
      }
    }
  })
})

describe('calculateLineTotals', () => {
  it('computes total and tax for a standard line', () => {
    // 1 x Consulting Hour @ 75, 23% tax - the seed data's first line
    expect(calculateLineTotals(1, 75, 23)).toEqual({ lineTotal: 75, lineTax: 17.25 })
  })

  it('computes correctly for quantities greater than 1', () => {
    // 4 x Software License @ 250, 23% tax
    expect(calculateLineTotals(4, 250, 23)).toEqual({ lineTotal: 1000, lineTax: 230 })
  })

  it('treats zero quantity as an empty line', () => {
    expect(calculateLineTotals(0, 75, 23)).toEqual({ lineTotal: 0, lineTax: 0 })
  })

  it('treats zero unit price as an empty line', () => {
    expect(calculateLineTotals(5, 0, 23)).toEqual({ lineTotal: 0, lineTax: 0 })
  })

  it('applies zero tax when the product has no tax rate', () => {
    expect(calculateLineTotals(2, 50, 0)).toEqual({ lineTotal: 100, lineTax: 0 })
  })

  it('treats missing/undefined/null inputs as zero rather than producing NaN', () => {
    expect(calculateLineTotals(undefined, undefined, undefined)).toEqual({ lineTotal: 0, lineTax: 0 })
    expect(calculateLineTotals(null, 75, 23)).toEqual({ lineTotal: 0, lineTax: 0 })
    expect(calculateLineTotals(3, null, 23)).toEqual({ lineTotal: 0, lineTax: 0 })
    expect(calculateLineTotals(3, 75, null)).toEqual({ lineTotal: 225, lineTax: 0 })
  })

  it('handles fractional unit prices that need rounding', () => {
    // 3 x 33.333 = 99.999, which rounds to 100.00
    const { lineTotal } = calculateLineTotals(3, 33.333, 0)
    expect(lineTotal).toBe(100)
  })

  it('handles tax rates with fractional percentages', () => {
    // 1 x 100 @ 19.6% (a real-world VAT rate) = 19.60 tax
    expect(calculateLineTotals(1, 100, 19.6)).toEqual({ lineTotal: 100, lineTax: 19.6 })
  })

  it('stays exact for large quantities at scale', () => {
    // 1,000,000 x 0.01 = 10,000.00 - a case where naive floating point
    // multiplication (0.01 isn't exactly representable in binary) could drift.
    expect(calculateLineTotals(1_000_000, 0.01, 23)).toEqual({ lineTotal: 10000, lineTax: 2300 })
  })

  it('computes tax from the already-rounded line total, not the raw product', () => {
    // Documents the implementation's chosen order of operations: lineTax is
    // round2(lineTotal * taxRate / 100), where lineTotal is itself already
    // rounded - not round2(quantity * unitPrice * taxRate / 100) computed on the
    // unrounded raw amount. For most inputs these coincide, but callers relying on
    // this module should know which one they get.
    const quantity = 1
    const unitPrice = 0.125 // raw product needs its own rounding decision
    const taxRate = 10

    const roundedLineTotal = round2(quantity * unitPrice)
    const expectedTax = round2(roundedLineTotal * taxRate / 100)

    const { lineTotal, lineTax } = calculateLineTotals(quantity, unitPrice, taxRate)
    expect(lineTotal).toBe(roundedLineTotal)
    expect(lineTax).toBe(expectedTax)
  })
})

describe('calculateInvoiceTotals', () => {
  it('returns zeroes for an invoice with no lines', () => {
    expect(calculateInvoiceTotals([])).toEqual({ subTotal: 0, totalTax: 0, grandTotal: 0 })
  })

  it('matches a single line exactly', () => {
    const line = calculateLineTotals(2, 75, 23)
    expect(calculateInvoiceTotals([line])).toEqual({
      subTotal: line.lineTotal,
      totalTax: line.lineTax,
      grandTotal: round2(line.lineTotal + line.lineTax),
    })
  })

  it('sums multiple lines correctly (the seeded 5-line invoice)', () => {
    const lines = [
      calculateLineTotals(1, 75, 23),
      calculateLineTotals(2, 250, 23),
      calculateLineTotals(3, 75, 23),
      calculateLineTotals(4, 250, 23),
      calculateLineTotals(5, 75, 23),
    ]

    const totals = calculateInvoiceTotals(lines)

    expect(totals.subTotal).toBe(2175)
    expect(totals.totalTax).toBe(500.25)
    expect(totals.grandTotal).toBe(2675.25)
  })

  it('treats lines missing lineTotal/lineTax as contributing zero', () => {
    const lines = [{ lineTotal: 100, lineTax: 23 }, {}, { lineTotal: 50, lineTax: 11.5 }]
    expect(calculateInvoiceTotals(lines)).toEqual({ subTotal: 150, totalTax: 34.5, grandTotal: 184.5 })
  })

  it('always satisfies grandTotal = subTotal + totalTax exactly', () => {
    const lines = [
      calculateLineTotals(3, 19.99, 23),
      calculateLineTotals(7, 4.5, 6),
      calculateLineTotals(1, 999.99, 21),
    ]

    const { subTotal, totalTax, grandTotal } = calculateInvoiceTotals(lines)
    expect(grandTotal).toBe(round2(subTotal + totalTax))
  })

  it('does not drift under floating-point summation for a large number of lines', () => {
    // 1,000 lines of 0.10 each is a textbook floating-point trap: naive repeated
    // addition of 0.1 in JS drifts (0.1 * 3 !== 0.3-style errors compound). Verify
    // the module's summation still lands on an exact 2-decimal result.
    const lines = Array.from({ length: 1000 }, () => calculateLineTotals(1, 0.1, 0))
    expect(calculateInvoiceTotals(lines).subTotal).toBe(100)
  })

  it('matches an independent integer-cents ground truth at invoice scale', () => {
    // Cross-check against a completely independent calculation path (integer
    // arithmetic in cents, which has no floating-point representation error for
    // values in this range) rather than re-deriving the same formula. If these
    // two disagree, the module has a real precision bug, not just a different
    // rounding convention.
    const seedProducts = [
      { unitPrice: 75, taxRate: 23 },
      { unitPrice: 250, taxRate: 23 },
      { unitPrice: 19.99, taxRate: 6 },
      { unitPrice: 4.49, taxRate: 13 },
    ]

    const lineCount = 5000
    const lines = []
    let expectedSubTotalCents = 0
    let expectedTaxCents = 0

    for (let i = 0; i < lineCount; i++) {
      const product = seedProducts[i % seedProducts.length]
      const quantity = (i % 9) + 1

      lines.push(calculateLineTotals(quantity, product.unitPrice, product.taxRate))

      const lineTotalCents = Math.round(quantity * product.unitPrice * 100)
      expectedSubTotalCents += lineTotalCents
      expectedTaxCents += Math.round((lineTotalCents * product.taxRate) / 100)
    }

    const totals = calculateInvoiceTotals(lines)

    expect(totals.subTotal).toBeCloseTo(expectedSubTotalCents / 100, 2)
    expect(totals.totalTax).toBeCloseTo(expectedTaxCents / 100, 2)
    expect(totals.grandTotal).toBe(round2(totals.subTotal + totals.totalTax))
  })
})
