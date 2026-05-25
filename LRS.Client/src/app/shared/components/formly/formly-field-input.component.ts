import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { FieldType, FieldTypeConfig, FormlyModule } from '@ngx-formly/core';

@Component({
  selector: 'app-formly-field-input',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormlyModule],
  template: `
    <div class="form-group">
      <label [for]="id">
        {{ props.label }}
        <span *ngIf="props.required" class="required-asterisk">*</span>
      </label>
      <input
        [type]="props.type || 'text'"
        [id]="id"
        [formControl]="formControl"
        [formlyAttributes]="field"
        [placeholder]="props.placeholder || ''"
      />
      <span class="error" *ngIf="showError">
        <formly-validation-message [field]="field"></formly-validation-message>
      </span>
    </div>
  `
})
export class FormlyFieldInputComponent extends FieldType<FieldTypeConfig> {}
