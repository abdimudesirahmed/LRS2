import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { FieldType, FieldTypeConfig, FormlyModule } from '@ngx-formly/core';

@Component({
  selector: 'app-formly-field-select',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormlyModule],
  template: `
    <div class="form-group">
      <label [for]="id">
        {{ props.label }}
        <span *ngIf="props.required" class="required">*</span>:
      </label>
      <div class="select-wrapper">
        <select
          [id]="id"
          [formControl]="formControl"
          [formlyAttributes]="field"
        >
          <option [value]="0" disabled>{{ props.placeholder || 'Select an option...' }}</option>
          <option *ngFor="let option of selectOptions" [value]="option.value">
            {{ option.label }}
          </option>
        </select>
      </div>
      <span class="loading" *ngIf="props['loading']">{{ props['loadingMessage'] || 'Loading...' }}</span>
      <span class="error" *ngIf="showError">
        <formly-validation-message [field]="field"></formly-validation-message>
      </span>
    </div>
  `
})
export class FormlyFieldSelectComponent extends FieldType<FieldTypeConfig> {
  get selectOptions(): any[] {
    return Array.isArray(this.props.options) ? this.props.options : [];
  }
}
