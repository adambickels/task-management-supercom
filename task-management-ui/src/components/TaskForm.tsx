import { useEffect, useState, useRef } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Button,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Chip,
  Box,
  Stack,
  OutlinedInput,
  type SelectChangeEvent,
} from '@mui/material';
import type { TaskItem, Tag, CreateTaskRequest } from '../types';
import { useAppDispatch } from '../store/hooks';
import { createTask, updateTask } from '../store/taskSlice';
import styles from './TaskForm.module.scss';

interface TaskFormProps {
  open: boolean;
  onClose: () => void;
  task: TaskItem | null;
  tags: Tag[];
}

const TaskForm = ({ open, onClose, task, tags }: TaskFormProps) => {
  const dispatch = useAppDispatch();
  const isSubmittingRef = useRef(false);
  const [formData, setFormData] = useState<CreateTaskRequest>({
    title: '',
    description: '',
    dueDate: '',
    priority: 3,
    fullName: '',
    telephone: '',
    email: '',
    tagIds: [],
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    // Defer state updates to avoid cascading renders
    if (open) {
      const timeoutId = setTimeout(() => {
        setIsSubmitting(false);
        if (task) {
          setFormData({
            title: task.title,
            description: task.description,
            dueDate: task.dueDate.split('T')[0] + 'T' + task.dueDate.split('T')[1].substring(0, 5),
            priority: task.priority,
            fullName: task.fullName,
            telephone: task.telephone,
            email: task.email,
            tagIds: task.tagIds,
          });
        } else {
          setFormData({
            title: '',
            description: '',
            dueDate: '',
            priority: 3,
            fullName: '',
            telephone: '',
            email: '',
            tagIds: [],
          });
        }
        setErrors({});
      }, 0);

      return () => clearTimeout(timeoutId);
    }
  }, [task, open]);

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.title.trim()) {
      newErrors.title = 'Title is required';
    } else if (formData.title.length > 200) {
      newErrors.title = 'Title cannot exceed 200 characters';
    }

    if (!formData.description.trim()) {
      newErrors.description = 'Description is required';
    } else if (formData.description.length > 2000) {
      newErrors.description = 'Description cannot exceed 2000 characters';
    }

    if (!formData.dueDate) {
      newErrors.dueDate = 'Due date is required';
    }

    if (!formData.fullName.trim()) {
      newErrors.fullName = 'Full name is required';
    } else if (formData.fullName.length > 100) {
      newErrors.fullName = 'Full name cannot exceed 100 characters';
    }

    if (!formData.telephone.trim()) {
      newErrors.telephone = 'Telephone is required';
    } else if (!/^[\d\-+()]+$/.test(formData.telephone)) {
      newErrors.telephone = 'Telephone can only contain numbers, +, -, and parentheses';
    } else if (formData.telephone.replace(/[-+()]/g, '').length < 7) {
      newErrors.telephone = 'Telephone must contain at least 7 digits';
    }

    if (!formData.email.trim()) {
      newErrors.email = 'Email is required';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = 'Invalid email address';
    }

    if (formData.tagIds.length === 0) {
      newErrors.tagIds = 'At least one tag is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    e.stopPropagation();

    // Use ref for immediate synchronous check - return immediately if already submitting
    if (isSubmittingRef.current) {
      return;
    }

    if (!validateForm()) {
      return;
    }

    // Set both ref (synchronous) and state (for UI)
    isSubmittingRef.current = true;
    setIsSubmitting(true);
    
    try {
      if (task) {
        await dispatch(updateTask({ id: task.id, task: formData })).unwrap();
      } else {
        await dispatch(createTask(formData)).unwrap();
      }
      onClose();
    } catch (error) {
      console.error('Error saving task:', error);
    } finally {
      // Small delay before resetting to prevent race conditions
      setTimeout(() => {
        isSubmittingRef.current = false;
        setIsSubmitting(false);
      }, 300);
    }
  };

  const handleChange = (field: keyof CreateTaskRequest, value: string | number | number[]) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  const handleTagChange = (event: SelectChangeEvent<number[]>) => {
    const value = event.target.value;
    handleChange('tagIds', typeof value === 'string' ? value.split(',').map(Number) : value);
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <form onSubmit={handleSubmit}>
        <DialogTitle>{task ? 'Edit Task' : 'Create New Task'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} className={styles.formContent}>
            <TextField
              label="Title"
              fullWidth
              required
              value={formData.title}
              onChange={(e) => handleChange('title', e.target.value)}
              error={!!errors.title}
              helperText={errors.title}
            />

            <TextField
              label="Description"
              fullWidth
              required
              multiline
              rows={4}
              value={formData.description}
              onChange={(e) => handleChange('description', e.target.value)}
              error={!!errors.description}
              helperText={errors.description}
            />

            <Box className={styles.fieldRow}>
              <TextField
                label="Due Date"
                type="datetime-local"
                fullWidth
                required
                value={formData.dueDate}
                onChange={(e) => handleChange('dueDate', e.target.value)}
                error={!!errors.dueDate}
                helperText={errors.dueDate}
                InputLabelProps={{
                  shrink: true,
                }}
              />

              <FormControl fullWidth>
                <InputLabel>Priority</InputLabel>
                <Select
                  value={formData.priority}
                  label="Priority"
                  onChange={(e) => handleChange('priority', Number(e.target.value))}
                >
                  <MenuItem value={1}>Very Low</MenuItem>
                  <MenuItem value={2}>Low</MenuItem>
                  <MenuItem value={3}>Medium</MenuItem>
                  <MenuItem value={4}>High</MenuItem>
                  <MenuItem value={5}>Critical</MenuItem>
                </Select>
              </FormControl>
            </Box>

            <FormControl fullWidth error={!!errors.tagIds}>
              <InputLabel>Tags</InputLabel>
              <Select
                multiple
                value={formData.tagIds}
                onChange={handleTagChange}
                input={<OutlinedInput label="Tags" />}
                renderValue={(selected) => (
                  <Box className={styles.chipBox}>
                    {selected.map((value) => {
                      const tag = tags.find((t) => t.id === value);
                      return <Chip key={value} label={tag?.name} size="small" />;
                    })}
                  </Box>
                )}
              >
                {tags.map((tag) => (
                  <MenuItem key={tag.id} value={tag.id}>
                    {tag.name}
                  </MenuItem>
                ))}
              </Select>
              {errors.tagIds && (
                <div className={styles.errorText}>
                  {errors.tagIds}
                </div>
              )}
            </FormControl>

            <TextField
              label="Full Name"
              fullWidth
              required
              value={formData.fullName}
              onChange={(e) => handleChange('fullName', e.target.value)}
              error={!!errors.fullName}
              helperText={errors.fullName}
            />

            <Box className={styles.fieldRow}>
              <TextField
                label="Telephone"
                fullWidth
                required
                value={formData.telephone}
                onChange={(e) => {
                  // Only allow digits, +, -, and parentheses (no spaces)
                  const value = e.target.value.replace(/[^\d\-+()]/g, '');
                  handleChange('telephone', value);
                }}
                error={!!errors.telephone}
                helperText={errors.telephone}
                placeholder="+1-234-567-8900"
              />

              <TextField
                label="Email"
                fullWidth
                required
                type="email"
                value={formData.email}
                onChange={(e) => handleChange('email', e.target.value)}
                error={!!errors.email}
                helperText={errors.email}
              />
            </Box>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} disabled={isSubmitting}>Cancel</Button>
          <Button type="submit" variant="contained" color="primary" disabled={isSubmitting}>
            {isSubmitting ? 'Saving...' : task ? 'Update' : 'Create'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
};

export default TaskForm;
