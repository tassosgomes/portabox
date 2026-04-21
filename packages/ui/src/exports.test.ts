import { describe, it, expect } from 'vitest'
import * as UI from './index'

describe('packages/ui public API exports', () => {
  it('exports Button component', () => {
    expect(UI.Button).toBeDefined()
    expect(typeof UI.Button).toBe('object') // forwardRef returns an object
  })

  it('exports Input component', () => {
    expect(UI.Input).toBeDefined()
  })

  it('exports Card component', () => {
    expect(UI.Card).toBeDefined()
    expect(typeof UI.Card).toBe('function')
  })

  it('exports Badge component', () => {
    expect(UI.Badge).toBeDefined()
    expect(typeof UI.Badge).toBe('function')
  })

  it('exports Modal component', () => {
    expect(UI.Modal).toBeDefined()
    expect(typeof UI.Modal).toBe('function')
  })

  it('exports StepIndicator component', () => {
    expect(UI.StepIndicator).toBeDefined()
    expect(typeof UI.StepIndicator).toBe('function')
  })

  it('exports Tree and TreeNode components', () => {
    expect(UI.Tree).toBeDefined()
    expect(typeof UI.Tree).toBe('function')
    expect(UI.TreeNode).toBeDefined()
    expect(typeof UI.TreeNode).toBe('object')
  })

  it('exports ConfirmModal component', () => {
    expect(UI.ConfirmModal).toBeDefined()
    expect(typeof UI.ConfirmModal).toBe('function')
  })

  it('exports all canonical Lucide icons', () => {
    expect(UI.Building2).toBeDefined()
    expect(UI.ChevronDown).toBeDefined()
    expect(UI.ChevronRight).toBeDefined()
    expect(UI.FileText).toBeDefined()
    expect(UI.Home).toBeDefined()
    expect(UI.UserPlus).toBeDefined()
    expect(UI.Mail).toBeDefined()
    expect(UI.CheckCircle).toBeDefined()
    expect(UI.UploadCloud).toBeDefined()
    expect(UI.ClipboardCheck).toBeDefined()
    expect(UI.Power).toBeDefined()
  })
})
